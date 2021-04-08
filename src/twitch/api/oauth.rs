use anyhow::Result;
use serde::Deserialize;

use super::CLIENT;

#[derive(Debug, Deserialize)]
pub struct Tokens {
  pub access_token: String,
  pub refresh_token: String,
  pub expires_in: i32,
  pub scope: Vec<String>,
  pub token_type: String,
}

#[derive(Debug, Deserialize)]
pub struct ValidationResult {
  pub client_id: String,
  pub login: String,
  pub scopes: Vec<String>,
  pub user_id: String,
  pub expires_in: i32,
}

pub async fn get_tokens(
  client_id: String,
  client_secret: String,
  code: String,
  redirect_uri: String,
) -> Result<Tokens> {
  Ok(
    CLIENT
      .post("https://id.twitch.tv/oauth2/token")
      .form(&[
        ("client_id", client_id),
        ("client_secret", client_secret),
        ("code", code),
        ("grant_type", "authorization_code".to_string()),
        ("redirect_uri", redirect_uri),
      ])
      .send()
      .await?
      .json()
      .await?,
  )
}

pub async fn validate(token: &String) -> Result<ValidationResult> {
  Ok(
    CLIENT
      .get("https://id.twitch.tv/oauth2/validate")
      .header("Authorization", format!("OAuth {}", token))
      .send()
      .await?
      .json()
      .await?,
  )
}
