use chrono::{DateTime, Utc};
use sqlx::{Executor, Postgres};
use twitch_irc::login::UserAccessToken as IrcAccessToken;
use uuid::Uuid;

#[derive(Debug)]
pub struct UserAccessToken {
  pub id: Uuid,
  pub user_id: i64,
  pub access_token: String,
  pub refresh_token: String,
  pub expires_on: Option<DateTime<Utc>>,
  pub created_on: DateTime<Utc>,
}

impl UserAccessToken {
  /// Gets the latest set of tokens from the database, or `None` if there are none available.
  pub async fn get_latest(
    executor: impl Executor<'_, Database = Postgres>,
  ) -> Result<Option<Self>, sqlx::Error> {
    Ok(
      sqlx::query_as!(
        UserAccessToken,
        r#"
SELECT * FROM user_access_token
ORDER BY created_on DESC
LIMIT 1
        "#
      )
      .fetch_optional(executor)
      .await?,
    )
  }

  pub async fn add(
    executor: impl Executor<'_, Database = Postgres>,
    user_id: i64,
    tokens: &IrcAccessToken,
  ) -> Result<Self, sqlx::Error> {
    Ok(
      sqlx::query_as!(
        UserAccessToken,
        r#"
INSERT INTO user_access_token (user_id, access_token, refresh_token, expires_on, created_on)
VALUES ($1, $2, $3, $4, $5)
RETURNING *
        "#,
        user_id,
        tokens.access_token,
        tokens.refresh_token,
        tokens.expires_at,
        tokens.created_at
      )
      .fetch_one(executor)
      .await?,
    )
  }
}
