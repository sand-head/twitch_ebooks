-- extension adds guid/uuid support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE twitch_channel (
  id BIGINT PRIMARY KEY,
  created_on TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_on TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE twitch_message (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  channel_id BIGINT REFERENCES twitch_channel (id) NOT NULL,
  user_id BIGINT NOT NULL,
  message TEXT NOT NULL,
  created_on TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_on TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE user_access_token (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  user_id BIGINT NOT NULL,
  access_token TEXT NOT NULL,
  refresh_token TEXT NOT NULL,
  expires_on TIMESTAMPTZ,
  created_on TIMESTAMPTZ NOT NULL DEFAULT NOW()
);