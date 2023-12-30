This Rust plugin allows the Rust server to send notifications to Telegram about raid events, including destruction of buildings and doors

# Description:

Send notifications to Telegram about building destruction.
Door destruction notifications, including door types.
Support for customisable bot token for Telegram API.
Ability to link game Steam account with Telegram ID for personalised notifications.

# Settings:
To customise the plugin, you need to specify the bot token for Telegram API in the configuration file.

```
{ "bot_token": "YOUR_BOT_TOKEN" }
```

# Commands:

* `/connect [telegram id]` - Start linking your Telegram account (It is necessary that the user is authorised in the bot in advance)
* `/check [code]` - End account linking
 

***Author: promise***