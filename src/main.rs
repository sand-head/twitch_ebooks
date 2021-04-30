use std::{
  collections::HashMap,
  convert::TryInto,
  sync::{Arc, Mutex},
  time::Duration,
};

use anyhow::Result;
use database::models::{TwitchChannel, TwitchMessage, UserAccessToken};
use dotenv::{dotenv, from_filename};
use itertools::Itertools;
use lazy_static::lazy_static;
use markov::MarkovChain;
use sqlx::PgPool;
use twitch::{
  api::{
    oauth::{self, validate},
    users,
  },
  tokens::PostgresTokenStorage,
};
use twitch_irc::{
  login::{RefreshingLoginCredentials, UserAccessToken as IrcAccessToken},
  message::ServerMessage,
  ClientConfig, TCPTransport, TwitchIRCClient,
};

mod database;
mod http;
mod markov;
mod twitch;

lazy_static! {
  static ref ENV: &'static str = if cfg!(test) {
    "test"
  } else if cfg!(debug_assertions) {
    "development"
  } else {
    "production"
  };
}

type MarkovMap = Arc<Mutex<HashMap<String, MarkovChain>>>;

fn format_auth_url(scopes: Vec<&'static str>) -> Result<String> {
  let scopes: String = scopes.into_iter().intersperse(" ").collect();
  let url = url::Url::parse_with_params(
    "https://id.twitch.tv/oauth2/authorize",
    &[
      ("client_id", std::env::var("TWITCH_CLIENT_ID")?),
      ("redirect_uri", std::env::var("TWITCH_REDIRECT_URI")?),
      ("response_type", "code".to_string()),
      ("scope", scopes),
    ],
  )?;
  Ok(url.to_string())
}

#[tokio::main]
async fn main() -> Result<()> {
  // load .env
  from_filename(format!(".env.{}.local", *ENV)).ok();
  from_filename(".env.local").ok();
  from_filename(format!(".env.{}", *ENV)).ok();
  dotenv()?;

  // create database
  let db_url = std::env::var("DATABASE_URL")?;
  database::create_database_if_not_exists(&db_url).await?;
  let pool = PgPool::connect(&db_url).await?;
  sqlx::migrate!("./migrations").run(&pool).await?;

  // get latest auth tokens
  let mut conn = pool.acquire().await?;
  let mut auth_tokens = UserAccessToken::get_latest(&mut conn).await?;
  // if none available, pop open the http server so we can get some from twitch
  if auth_tokens.is_none() {
    // todo this looks ugly clean up pls
    println!("hey so we don't have any twitch access tokens, mind running out and getting some?");
    println!("I'll go ahead and open up the endpoint for you, but in the meantime, feel free to head over here:");
    println!("{}", format_auth_url(vec!["chat:read", "chat:edit"])?);
    let tokens = http::expose_endpoint_and_wait_for_tokens().await?;
    println!("ok we have tokens now, thank you");
    let created_at = chrono::Utc::now();
    let expires_at =
      created_at + chrono::Duration::from_std(Duration::from_secs(tokens.expires_in.try_into()?))?;
    let validated = oauth::validate(&tokens.access_token).await?;
    auth_tokens = Some(
      UserAccessToken::add(
        &mut conn,
        validated.user_id.parse::<i64>()?,
        &IrcAccessToken {
          access_token: tokens.access_token,
          refresh_token: tokens.refresh_token,
          created_at,
          expires_at: Some(expires_at),
        },
      )
      .await?,
    );
  }

  // get bot user details using auth tokens
  let auth_tokens = auth_tokens.unwrap();
  let user = users::get_user(auth_tokens.access_token.as_str(), auth_tokens.user_id)
    .await?
    .expect("Could not get details of bot user");
  let user_id = user.id.parse::<i64>()?;
  println!("Got bot user details for login");

  // get all connected channels
  let channels = TwitchChannel::list(&mut conn).await?;
  let channel_users = users::get_users(
    &auth_tokens.access_token,
    channels.iter().map(|c| c.id).collect(),
  )
  .await?;

  // set up markov chains for connected channels
  let chains: MarkovMap = Arc::new(Mutex::new(HashMap::new()));
  for channel in channel_users.clone() {
    let chains = chains.clone();
    let pool = pool.clone();
    tokio::spawn(async move {
      // get a db connection and this channel's messages
      let mut conn = pool.acquire().await.unwrap();
      let channel_id = channel.id.parse::<i64>().unwrap();
      let messages = TwitchMessage::list(&mut conn, channel_id)
        .await
        .expect("Could not retrieve messages from database");

      // create a chain, stuff it full of messages
      let mut chain = MarkovChain::default();
      for message in messages {
        chain.add(message.message);
      }
      chains.lock().unwrap().insert(channel.login, chain);
    });
  }

  // set up twitch connection
  let client_id = std::env::var("TWITCH_CLIENT_ID")?;
  let client_secret = std::env::var("TWITCH_CLIENT_SECRET")?;
  let config = ClientConfig::new_simple(RefreshingLoginCredentials::new(
    user.login,
    client_id,
    client_secret,
    PostgresTokenStorage::new(user_id, pool.clone()),
  ));
  let (mut incoming, client) = TwitchIRCClient::<TCPTransport, _>::new(config);

  let incoming_handle = tokio::spawn(async move {
    while let Some(message) = incoming.recv().await {
      // todo: match incoming messages for interesting ones
      // also, parse out commands and junk
      match message {
        ServerMessage::Privmsg(message) => {
          if message.message_text.starts_with("~generate") {
            println!("Oh hey! A generate request!");
          } else if message.message_text.starts_with("~join") {
            println!("Oh hey! A join request!");
          } else {
            println!("This one goes in the chain bin!");
          }
        }
        _ => {}
      }
    }
  });

  // actually join each channel
  for channel in channel_users {
    client.join(channel.login.clone());
    println!("Joined channel {}", channel.login);
  }

  incoming_handle.await?;
  Ok(())
}
