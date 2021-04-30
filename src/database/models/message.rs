use chrono::{DateTime, Utc};
use sqlx::{Executor, Postgres};
use uuid::Uuid;

#[derive(Debug)]
pub struct TwitchMessage {
  pub id: Uuid,
  pub channel_id: i64,
  pub user_id: i64,
  pub message: String,
  pub created_on: DateTime<Utc>,
  pub updated_on: DateTime<Utc>,
}

impl TwitchMessage {
  pub async fn list(
    executor: impl Executor<'_, Database = Postgres>,
    channel_id: i64,
  ) -> Result<Vec<Self>, sqlx::Error> {
    Ok(
      sqlx::query_as!(
        TwitchMessage,
        r#"
SELECT * FROM twitch_message
WHERE channel_id = $1;
        "#,
        channel_id
      )
      .fetch_all(executor)
      .await?,
    )
  }
}
