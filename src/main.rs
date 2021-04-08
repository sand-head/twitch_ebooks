use std::collections::HashMap;

use anyhow::Result;
use database::models::UserAccessToken;
use dotenv::{dotenv, from_filename};
use lazy_static::lazy_static;
use markov::MarkovChain;
use sqlx::PgPool;
use twitch::tokens::PostgresTokenStorage;
use twitch_irc::{
  login::RefreshingLoginCredentials, message::ServerMessage, ClientConfig, TCPTransport,
  TwitchIRCClient,
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

struct App {
  chains: HashMap<String, MarkovChain>,
}
impl Default for App {
  fn default() -> Self {
    Self {
      chains: HashMap::new(),
    }
  }
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

  // set up app state
  let mut app = App::default();

  // get latest auth tokens
  let mut conn = pool.acquire().await?;
  let auth_tokens = UserAccessToken::get_latest(&mut conn).await?;
  // if none available, pop open the http server so we can get some from twitch
  if auth_tokens.is_none() {
    println!("hey so we don't have any twitch access tokens, mind running out and getting some?");
    println!("I'll go ahead and open up the endpoint for you...");
    http::expose_oauth_endpoint().await?;
  }

  // set up twitch connection
  // todo: use our own twitch account instead of an anonymous one
  let client_id = std::env::var("TWITCH_CLIENT_ID")?;
  let client_secret = std::env::var("TWITCH_CLIENT_SECRET")?;
  let config = ClientConfig::new_simple(RefreshingLoginCredentials::new(
    "todo put login here".to_string(),
    client_id,
    client_secret,
    PostgresTokenStorage::new(1, pool.clone()),
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
          } else {
            println!("This one goes in the chain bin!");
          }
        }
        _ => {}
      }
    }
  });

  // todo: join all channels
  app
    .chains
    .insert("sand_head".to_owned(), MarkovChain::default());
  client.join("sand_head".to_owned());

  incoming_handle.await?;
  Ok(())
}
