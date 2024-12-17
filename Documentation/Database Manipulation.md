# Database Manipulation

There are some features of the server that we currently don't have an API for, so you'll need to manually alter the database to fully use them. This document will describe some of these features, and how exactly you should modify database tables to use them.

## Admin Sims

Users and Avatars can both be given moderation powers via `moderation_level` on both tables. These give selected users the following abilities:

### Avatar `moderation_level`

A non-zero `moderation_level` allows a player to do the following in the city:

- Displays a MOMI badge on their sim page
- Enter and hold open any property on the map, even if they are not a roommate
- Enter properties at or above sim capacity
- Post bulletin board messages anywhere and with no restriction after moving
- Purchase properties in reserved neighbourhoods
- Delete bulletin board posts
- Delete ratings

In the lot, it gives users `VMTSOAvatarPermissions.Admin`, which does the following:

- Displays a MOMI badge on their icon in Live Mode
- An admin prefix on their name, red chat balloon
- Access to the Admin chat channel
- Can perform any action that lot owners and roommates can, on all properties.
- Allows trading of restricted items (catalog disable level > 1)
- Allows access and placement of Debug objects (hold shift when clicking the terrain button in Build Mode)
- Allows access to Debug interactions
  - Some objects, like the MOMI Station, have most of their functionality as debug interactions
  - These interactions can be more debug than moderation, so don't be surprised if weird things happen choosing certain options.
- Allows access to "CSR" (customer service representative) interactions
- Always allows moving and deletion of all objects, even when they are in use or the object tries to disallow it (such as trash)
- Always allows purchase of _any_ object, even ones not in the catalog. You can spawn these using Volcanic, as it uses the buy mode purchase request.
- Can run serverside chat commands, such as `/setjob`.

To do some of these actions, you must enter moderation mode in the client. Press CTRL-F1, then press M when the debug menu is visible. This will disable client-side checks for a lot of things, but the server will still reject you if you don't have the correct permissions level.

### User `is_admin` and `is_moderator`

The user permissions are more oriented around API access. The `is_moderator` field allows access to:

- Ingame actions for ban, ip ban and kick of avatars (available in moderation mode on the person page)
- Access to the administration API:
  - Can announce messages to all shards (cities).
  - Can list deployed and incoming updates.
  - Can list and manage ingame events.
  - Can list users, ban/unban and kick them from the game.
  - Can request a lot with a 3d thumbnail that needs to be calculated from the server. The lot returned will lose its 3d dirty flag.
  - Can request a lot FSOV from the server.
  - Can upload a 3d thumbnail (lot facade, `FSOF`) to the server. 
- Access to the server when `maintainance` is set to `true` in the API config.

`is_admin` allows access to these additional capabilities:
- Can list server hosts (city servers, lot servers etc)
- Can list and request shut down of shards (cities).
- Can list and dispatch tasks manually, such as top 100 calculation.
- Can manage updates, update branches and update addons. See the Admin Tool docs for more info.
- Can create a user using the Admin API, with admin and mod rights.
- Can send ingame mail to sims using the Admin API. Useful for integration with external services.
- Can delete avatars under a week old from Select a Sim.


A user's `is_moderator` field also affects the `moderation_level` of any avatars they create in future. Changing it will not update their existing avatar's `moderation_level`, you must do that manually.

### Chat commands

TODO

## Events

TODO

## Special Lot Types

TODO