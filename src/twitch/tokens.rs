use async_trait::async_trait;
use sqlx::PgPool;
use twitch_irc::login::{TokenStorage, UserAccessToken};

use crate::database::models::UserAccessToken as DbAccessToken;

#[derive(Debug)]
pub struct PostgresTokenStorage {
  user_id: i64,
  pool: PgPool,
}
impl PostgresTokenStorage {
  pub fn new(user_id: i64, pool: PgPool) -> Self {
    Self { user_id, pool }
  }
}

#[async_trait]
impl TokenStorage for PostgresTokenStorage {
  type LoadError = sqlx::Error;
  type UpdateError = sqlx::Error;

  async fn load_token(&mut self) -> Result<UserAccessToken, Self::LoadError> {
    let mut conn = self.pool.acquire().await?;
    let tokens = DbAccessToken::get_latest(&mut conn).await?.unwrap();

    Ok(UserAccessToken {
      access_token: tokens.access_token,
      refresh_token: tokens.refresh_token,
      created_at: tokens.created_on,
      expires_at: tokens.expires_on,
    })
  }

  async fn update_token(&mut self, token: &UserAccessToken) -> Result<(), Self::UpdateError> {
    let mut conn = self.pool.acquire().await?;
    DbAccessToken::add(&mut conn, self.user_id, token).await?;
    Ok(())
  }
}
