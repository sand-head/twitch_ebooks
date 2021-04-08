use lazy_static::lazy_static;
use serde::Deserialize;

pub mod oauth;
pub mod users;

lazy_static! {
  pub(in crate::twitch::api) static ref CLIENT: reqwest::Client = reqwest::Client::new();
}

#[derive(Debug, Deserialize)]
pub(in crate::twitch::api) struct Data<T> {
  pub data: Vec<T>,
}
