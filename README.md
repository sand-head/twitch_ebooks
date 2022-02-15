# twitch_ebooks
[![Matrix](https://img.shields.io/matrix/twitch_ebooks:schweigert.dev?server_fqdn=matrix.schweigert.dev&style=flat-square)](https://matrix.to/#/#twitch_ebooks:schweigert.dev)

A Markov chain bot for Twitch.

## Usage

This bot is hosted and live under the Twitch username `twitch_ebooks`.
There are various commands that may be used in either the bot's chat or a user's chat (after it joins):

### Bot's Chat Commands

1. `~join`: Makes the bot join your chat and start monitoring.
2. `~leave`: Makes the bot leave your chat, purging all of the data collected.

### User's Chat Commands

1. `~generate`: The bot uses previously submitted chat messages to generate a new message, and then sends it in chat.
2. `~leave`: Also makes the bot leave your chat, and can only be used by the broadcaster themselves.
3. `~ignore [username]`: Ignores a given user, deleting all their previous messages from the database.
4. `~purge [word]`: Removes all messages with a given word from the database.
