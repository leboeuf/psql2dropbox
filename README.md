# psql2dropbox

Dumps a PostgreSQL database to a Dropbox folder.

## Usage

`./psql2dropbox --connectionstring=... --dropboxfoldername=... --apitoken=...`

**connectionstring**: The **base64** connection string of the PostgreSQL database to back up.

**dropboxfoldername**: The folder to which the backup will be uploaded. This must correspond to the app name registered in the Dropbox App Console (see next section). The actual destination folder will be `/Apps/[dropboxfoldername]`, as per Dropbox's convention.

**apitoken**: The access token for this Dropbox app (see next section).

## Dropbox

Follow these steps to generate an app and access token:

1. Go to the [App Console](https://www.dropbox.com/developers/apps)
1. Click Create App
1. Select type of access: `App folder`
1. Set the name of the app, i.e. the name of the destination folder
1. Click Create app
1. Under "Settings", add `https://www.dropbox.com/1/oauth2/display_token` as a OAuth2 Redirect URI for the app
1. Under "Permissions", enable the `files.content.write` scope
1. Generate an access token
    * Option 1: Generate a short-lived (4h) token in the browser:
        1. In a web broswer, while signed-in to the user account where the backups will be stored, navigate to https://www.dropbox.com/oauth2/authorize?response_type=token&redirect_uri=https://www.dropbox.com/1/oauth2/display_token&client_id=APPKEYHERE` (replace APPKEYHERE by the app key found on the Settings page)
        1. After authorizing the app, the access token will be displayed
    * Option 2: Generate a short-lived (4h) token in the App Console:
        1. Under "Settings", set "Access token expiration" to "Short-lived" and click "Generate access token". The token will start with "sl."
    * Option 3: Generate a long-lived token in the App Console:
        1. Under "Settings", set "Access token expiration" to "No expiration" and click "Generate".

This will allow the access token to read/write to `/Apps/[name of the app]`
