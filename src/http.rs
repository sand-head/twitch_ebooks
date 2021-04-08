use std::{convert::Infallible, net::SocketAddr, sync::Arc};

use anyhow::Result;
use hyper::{
  service::{make_service_fn, service_fn},
  Body, Request, Response, Server,
};
use lazy_static::lazy_static;
use tokio::sync::{oneshot, Mutex};

lazy_static! {
  static ref TX: Arc<Mutex<Option<oneshot::Sender<()>>>> = <_>::default();
}

async fn oauth(_: Request<Body>) -> Result<Response<Body>, Infallible> {
  // todo: handle twitch oauth response
  if let Some(tx) = TX.lock().await.take() {
    // obtain sender from mutex and send void
    tx.send(());
  }
  Ok(Response::new("Hello, world!".into()))
}

pub async fn expose_oauth_endpoint() -> Result<()> {
  // bind to port 8080
  let addr = SocketAddr::from(([0, 0, 0, 0], 8080));

  // create oneshot channel
  let (sender, receiver) = oneshot::channel::<()>();
  TX.lock().await.replace(sender);

  // create the server that serves the oauth endpoint
  let service = make_service_fn(|_conn| async { Ok::<_, Infallible>(service_fn(oauth)) });
  let server = Server::bind(&addr).serve(service);

  println!("Listening on http://{}", addr);
  let graceful_server = server.with_graceful_shutdown(async {
    receiver.await.ok();
  });

  // run server until our one request is handled
  if let Err(e) = graceful_server.await {
    eprintln!("Server error: {}", e);
  }

  Ok(())
}
