# Registration

The FreeSO official server's registration was handled by an external service that directly modified the database (`beta.freeso.org`), but the server does support registration with and without email verification out of the box.

Registration/password reset requests can be submitted to the API from anywhere, including a web browser. It is essential that this API is using HTTPS if used over the internet.

This means that you can create a registration page, host it anywhere and it can post requests directly to the game server. We don't have example pages for this right now, but the API is very simple, and these things should be tailored to your server anyways.

It is highly recommended to set up an SMTP server for the game server to send email verification for email and password reset. This prevents users from creating a ton of duplicate accounts (as easily) and allows users to reset their password via email if they forget it, which tends to happen more often than a password change where they remember the old one. It also means that you can use their email to send other messages, if you properly tell people that you'll do that via your Privacy Policy and have a way to opt out.

## Endpoints

All registration related requests come through the `RegistrationController` and `PasswordController` in `FSO.Server.Api.Core`, with `application/x-www-form-urlencoded` format for `POST` data and JSON response.

Here's a list of endpoints:

- Registration
  - `POST userapi/registration`: Account registration without email verification
    - Expects `username`, `email`, `password`, `key` in a form body.
    - `key` should be the same as `regkey` in the API config, if present. Use this to gate access to the server API for registration if you want to put another service in front of it (the key should only be known by your backend service and the FSO server - might misbehave with the timeout right now though).
    - Only works if API config `SmtpEnabled` is false or not present.
    - Errors:
      - `registration_failed`, `missing_registration_token`: SMTP is enabled, so this endpoint cannot be used.
      - `bad_request`, `user_short`/`user_long`/`user_invalid`/`pass_required`/`email_invalid`: Invalid registration form.
      - `key_wrong`: `regkey` is in config and was not provided as part of the request.
      - `registration_failed`, `ip_banned`: User is IP banned, cannot register new accounts.
      - `registration_failed`, `registrations_too_frequent`: User is registering too frequently.
      - `registration_failed`, `user_exists`: A user already exists with this username, or some unspecified error happened when inserting it into the database.
      - Responds with the user model from database if everything succeeded.
  - `POST userapi/registration/request`: Account registration request with email verification
    - Expects `email`, `confirmation_url` in a form body.
      - The `confirmation_url` should be a page that lets the user submit a username/password to register with the supplied token. If `%token%` is present in the URL, it will be replaced with the real token in the email. Use this to create a URL with the token as a query, for example: `https://freeso.org/registration/confirm?token=%token%`.
    - Will send an email to the requested address with a confirmation link. This is created from `MailRegistrationToken.html`.
    - Errors:
      - `registration_failed`, `smtp_disabled`: SMTP is disabled, so this endpoint cannot be used.
      - `registration_failed`, `missing_fields`: `confirmation_url` or `email` are missing.
      - `registration_failed`, `email_invalid`: `email` is not a valid email.
      - `registration_failed`, `email_taken`: `email` is already registered under another account.
      - `registration_failed`, `confirmation_pending`: A confirmation is already pending for this email.
      - `{ status: 'success' }`: Everything worked.
      - `{ status: 'email_failed' }`: Everything worked, but the actual confirmation email couldn't be sent.
  - `POST userapi/registration/confirm`: Account registration confirmation from email verification
    - Expects `username`, `password`, `key`, `token` in a form body.
    - Uses the email associated with the token to register the user account.
    - `token` must be the user's email confirmation token. It's only removed after success.
    - `key` should be the same as `regkey` in the API config, if present. Use this to gate access to the server API for registration if you want to put another service in front of it (the key should only be known by your backend service and the FSO server).
    - Will send an email to the user confirming the account creation. This is created from `MailRegistrationOK.html`.
    - Errors:
      - `registration_failed`, `invalid_token`: Couldn't find the email for the given token.
      - `bad_request`, `user_short`/`user_long`/`user_invalid`/`pass_required`/`email_invalid`: Invalid registration form.
      - `key_wrong`: `regkey` is in config and was not provided as part of the request.
      - `registration_failed`, `ip_banned`: User is IP banned, cannot register new accounts.
      - `registration_failed`, `registrations_too_frequent`: User is registering too frequently.
      - `registration_failed`, `user_exists`: A user already exists with this username, or some unspecified error happened when inserting it into the database.
      - Responds with the user model from database if everything succeeded.

- Password change/reset
  - `POST userapi/password`: Password change without email verification
    - Expects `username`, `old_password`, `new_password` in a form body.
    - Only works if API config `SmtpEnabled` is false or not present.
    - Errors:
      - `password_reset_failed`, `missing_confirmation_token`: SMTP is enabled, so this endpoint cannot be used.
      - `password_reset_failed`, `missing_fields`: `username`, `new_password` or `old_password` are missing.
      - `password_reset_failed`, `user_invalid`: A user identified by `username` cannot be found.
      - `password_reset_failed`, `incorrect_password`: `old_password` is not the current password for the user identified by `username`.
      - `{ status: 'success' }`: Everything worked.
  - `POST userapi/password/request`: Password reset request with email verification
    - Expects `email`, `confirmation_url` in a form body.
    - The `confirmation_url` should be a page that lets the user submit new password for the user with the supplied token. If `%token%` is present in the URL, it will be replaced with the real token in the email. Use this to create a URL with the token as a query, for example: `https://freeso.org/password_reset/confirm?token=%token%`.
    - Will send an email to the requested address with a confirmation link. This is created from `MailPasswordReset.html`.
    - Errors:
      - `password_reset_failed`, `smtp_disabled`: SMTP is disabled, so this endpoint cannot be used.
      - `password_reset_failed`, `missing_fields`: `confirmation_url` or `email` are missing.
      - `password_reset_failed`, `email_invalid`: The given `email` is either invalid or is not used by any user.
      - `password_reset_failed`, `confirmation_pending`: The user with the given email already has a password reset email that hasn't expired.
      - `{ status: 'success' }`: Everything worked.
      - `{ status: 'email_failed' }`: Everything worked, but the actual confirmation email couldn't be sent.
  - `POST userapi/password/confirm`: Password reset from email verification
    - Expects `token`, `new_password` in a form body.
    - `token` must be the user's email confirmation token. It's only removed after success.
    - The password will be changed to `new_password` if the confirmation token matches.
    - Will send an email to the user confirming the password reset. This is created from `MailPasswordResetOK.html`.
    - Errors:
      - `password_reset_failed`, `missing_fields`: `token` or `new_password` are missing.
      - `password_reset_failed`, `invalid_token`: Couldn't find the email for the given token.
      - `{ status: 'success' }`: Everything worked.

## Mail Server Configuration

If you have an SMTP server that you can use to send mail, then you can tell the server about it to use the email verification registration and password reset.

Fill out the following fields in your API configuration:

- `smtpEnabled`: If true, registration and password change will require email confirmation.
- `smtpHost`: Hostname for the SMTP server.
- `smtpPort`: Post for the SMTP server.
- `smtpUser`: Username for SMTP server.
- `smtpPassword`: Password for SMTP server.

### SMTP servers and Junk Mail

If you're running your own SMTP server, make sure you've properly set up your DNS, DKIM, SPF and DMARC to help mail servers verify that your mail is coming from the right place, and not just spoofed for spam email. This will require changing the configuration for both your SMTP server, and your DNS records.

You should google around to see exactly how this is set up for your mail server in particular. Some mail servers may reject future mail from you if you send test emails without proper configuration, so you _may_ need to contact email services to unblock you if you do make any missteps.

If you're using some external service to send mail, they will likely handle this for you - or bug you incessantly to configure it if you're trying to use a custom domain.

## Mail Templates

The email templates should be customized to fit your server and associated resources. (websites, support links, terms of service.)

Here's a list of the included templates, and the variables that can be injected into them:

- `MailBase.html`
  - Wrapper for all emails. Replaces `%content%` with the specific email content.
- `MailRegistrationToken.html`
  - Sent when a registration request is processed, includes a confirmation token that should allow the user to properly register.
  - `%confirmation_url%` - replaced with the confirmation URL with the token.
  - `%token%` - replaced with the token.
  - `%expires% - replaced with the expiry date of the token.
- `MailRegistrationOK.html`
  - Sent when a registration succeeds.
  - `%username%` - replaced with the registered username.
- `MailPasswordReset.html`
  - Sent when a password reset request is processed, includes a confirmation token that should allow the user to change their password.
  - `%confirmation_url%` - replaced with the confirmation URL with the token.
  - `%token%` - replaced with the token.
  - `%expires% - replaced with the expiry date of the token.
- `MailPasswordResetOK.html`
  - Sent when a password reset succeeds.
  - `%username%` - replaced with the username the password was changed for.
- `MailBan.html`
  - Sent when a user is banned via the Admin API.
  - `%username%` - replaced with the username for the account that was banned.
  - `%end%` - replaced with the end date of the ban.
- `MailUnban.html`
  - Unused.


