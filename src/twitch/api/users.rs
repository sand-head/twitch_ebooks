use anyhow::Result;
use chrono::{DateTime, Utc};
use serde::Deserialize;

use super::{Data, CLIENT};

#[derive(Clone, Debug, Deserialize)]
pub struct User {
  pub id: String,
  pub login: String,
  pub display_name: String,
  #[serde(rename = "type")]
  pub user_type: String,
  pub broadcaster_type: String,
  pub description: String,
  pub profile_image_url: String,
  pub offline_image_url: String,
  pub view_count: i32,
  pub email: Option<String>,
  pub created_at: DateTime<Utc>,
}

pub async fn get_user(token: &str, id: i64) -> Result<Option<User>> {
  Ok(get_users(token, vec![id]).await?.first().cloned())
}

pub async fn get_users(token: &str, ids: Vec<i64>) -> Result<Vec<User>> {
  Ok(
    CLIENT
      .get("https://api.twitch.tv/helix/users")
      .query(
        &ids
          .iter()
          .map(|id| ("id", id.to_string()))
          .collect::<Vec<_>>(),
      )
      .header("client-id", std::env::var("TWITCH_CLIENT_ID")?)
      .header("Authorization", format!("Bearer {}", token))
      .send()
      .await?
      .json::<Data<User>>()
      .await?
      .data,
  )
}
