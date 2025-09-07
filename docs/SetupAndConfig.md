
## User Password

git-taut is implemented as a remote helper, a middleman between the current repo and the remote. 
A user password is set for a remote, possibly with a user name which is used as salt for key derivation.
Once the user password is set for a remote, it cannot be changed. 
However, different user passwords can be used for different remotes.

The user name/password is retrieved with a git credential helper, usually from the credential manager you use.
A specific url is generated to help identify the user name/password in the credential manager.
Further more, a key trait is derived from the url as well as the user name/password to help verify the password input in the future.

## Taut related settings in the repo config

- `remote.{name}.tautCredentialUrl`: url that identifies this remote in a git credential helper.
- `remote.{name}.tautCredentialUserName`: optional user name from credential, used as salt for key derivation.
- `remote.{name}.tautCredentialKeyTrait`: key trait derived from the user password, used to verify future password inputs.
