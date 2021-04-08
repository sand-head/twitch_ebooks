use chrono::{DateTime, Utc};
use uuid::Uuid;

#[derive(Debug)]
pub struct TwitchMessage {
  pub id: Uuid,
  pub channel_id: i64,
  pub user_id: i64,
  pub message: String,
  pub expires_on: DateTime<Utc>,
  pub created_on: DateTime<Utc>,
}
