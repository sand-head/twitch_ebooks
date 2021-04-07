use std::collections::HashMap;

use anyhow::Result;
use markov::MarkovChain;
use twitch_irc::{
  login::StaticLoginCredentials, message::ServerMessage, ClientConfig, TCPTransport,
  TwitchIRCClient,
};

mod http;
mod markov;

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
  // set up app state
  let mut app = App::default();

  // todo: check if we need to re-up auth tokens
  // or if we don't have them in the first place
  if false {
    http::expose_oauth_endpoint().await?;
  }

  // set up twitch connection
  // todo: use our own twitch account instead of an anonymous one
  let config = ClientConfig::default();
  let (mut incoming, client) = TwitchIRCClient::<TCPTransport, StaticLoginCredentials>::new(config);

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
