use std::{collections::HashMap, convert::Infallible, net::SocketAddr, sync::Arc};

use anyhow::Result;
use hyper::{
  service::{make_service_fn, service_fn},
  Body, Request, Response, Server,
};
use lazy_static::lazy_static;
use tokio::sync::{oneshot, Mutex};

use crate::twitch::api::oauth::{self, Tokens};

lazy_static! {
  static ref TX: Arc<Mutex<Option<oneshot::Sender<Tokens>>>> = <_>::default();
}

async fn oauth(req: Request<Body>) -> Result<Response<Body>> {
  // get params from query string
  let params = req
    .uri()
    .query()
    .map(|query| {
      url::form_urlencoded::parse(query.as_bytes())
        .into_owned()
        .collect()
    })
    .unwrap_or_else(HashMap::new);
  let code = params.get("code").expect("No code param found").clone();

  // request some slick new tokens from twitch
  let client_id = std::env::var("TWITCH_CLIENT_ID")?;
  let client_secret = std::env::var("TWITCH_CLIENT_SECRET")?;
  let redirect_uri = std::env::var("TWITCH_REDIRECT_URI")?;
  let tokens = oauth::get_tokens(client_id, client_secret, code, redirect_uri).await?;

  if let Some(tx) = TX.lock().await.take() {
    // obtain sender from mutex and send our new tokens
    let _ = tx.send(tokens);
  }
  Ok(Response::new("Hello, world!".into()))
}

pub async fn expose_endpoint_and_wait_for_tokens() -> Result<Tokens> {
  // bind to port 8080
  let addr = SocketAddr::from(([0, 0, 0, 0], 8080));

  // create oneshot channel
  let (sender, receiver) = oneshot::channel::<Tokens>();
  TX.lock().await.replace(sender);

  // create the server that serves the oauth endpoint
  let service = make_service_fn(|_conn| async { Ok::<_, Infallible>(service_fn(oauth)) });
  let server = Server::bind(&addr).serve(service);

  println!("Listening for requests...");
  let mut tokens: Option<Tokens> = None;
  let graceful_server = server.with_graceful_shutdown(async {
    tokens = receiver.await.ok();
  });

  // run server until our one request is handled
  if let Err(e) = graceful_server.await {
    eprintln!("Server error: {}", e);
  }

  Ok(tokens.unwrap())
}
