use std::collections::HashMap;

use anyhow::Result;
use chain::Chain;

mod chain;
mod http;

struct App<'a> {
  chains: HashMap<u32, Chain<'a>>,
}
impl<'a> Default for App<'a> {
  fn default() -> Self {
    Self {
      chains: HashMap::new(),
    }
  }
}

#[tokio::main]
async fn main() -> Result<()> {
  println!("Hello, world!");

  let app = App::default();

  http::expose_oauth_endpoint().await?;

  Ok(())
}
