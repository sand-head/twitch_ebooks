use chrono::{DateTime, Utc};
use sqlx::{Executor, Postgres};

#[derive(Debug)]
pub struct TwitchChannel {
  pub id: i64,
  pub created_on: DateTime<Utc>,
  pub updated_on: DateTime<Utc>,
}

impl TwitchChannel {
  pub async fn list(
    executor: impl Executor<'_, Database = Postgres>,
  ) -> Result<Vec<Self>, sqlx::Error> {
    Ok(
      sqlx::query_as!(
        TwitchChannel,
        r#"
SELECT * FROM twitch_channel;
        "#,
      )
      .fetch_all(executor)
      .await?,
    )
  }
}
