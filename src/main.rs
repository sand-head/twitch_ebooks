use std::collections::HashMap;

use anyhow::Result;
use markov::MarkovChain;

mod http;
mod markov;

struct App {
  chains: HashMap<u32, MarkovChain>,
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
  println!("Hello, world!");

  let app = App::default();

  http::expose_oauth_endpoint().await?;

  Ok(())
}
