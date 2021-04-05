use std::{convert::Infallible, net::SocketAddr};

use anyhow::Result;
use hyper::{
  service::{make_service_fn, service_fn},
  Body, Request, Response, Server,
};

async fn oauth(_: Request<Body>) -> Result<Response<Body>, Infallible> {
  // todo: handle twitch oauth response
  Ok(Response::new("Hello, world!".into()))
}

pub async fn expose_oauth_endpoint() -> Result<()> {
  // bind to port 8080
  let addr = SocketAddr::from(([0, 0, 0, 0], 8080));

  // create a service from the `oauth` fn
  let oauth_svc = make_service_fn(|_| async { Ok::<_, Infallible>(service_fn(oauth)) });
  // create the server that serves the oauth endpoint
  let server = Server::bind(&addr).serve(oauth_svc);

  println!("Listening on http://{}", addr);
  server.await?;

  Ok(())
}
