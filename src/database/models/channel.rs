use chrono::{DateTime, Utc};

#[derive(Debug)]
pub struct TwitchChannel {
  pub id: i64,
  pub expires_on: DateTime<Utc>,
  pub created_on: DateTime<Utc>,
}
