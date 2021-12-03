# Credly
Credly hosts the largest and most-connected digital credential network. We help the world speak a common language of verified knowledge, skills, and abilities.

## Publisher: Troy Taylor, Hitachi Solutions

## Prerequisites
You can only use this connector if you have access to an organization's issuer. This connector does not work with badge earner accounts.

## Obtaining Credentials
This connector uses an access token. In your issuer account, you will generate a token for this connector. With this generated access token, you will use this as part of the Authorization header for this connector. When you authenticate the connector, you will need to enter it as 'Bearer [Your access token]' - make sure there is a space between bearer and your token.

## Supported Operations
### Get badge templates
Get all badge templates for an organization.
### Get a single badge template
Get a single badge template from an organization.
### Get issued badges
Get all issued badges for an organization.
### Issue a badge
Issue a badge to an earner. Archived badge template cannot be used to issue new badges.
### Get badges in bulk
Get all badges for an organization in bulk.
### Replace a badge
Replace a badge by creating a new badge to take the place of an existing badge. Fields that are not provided will not assume the values from the replaced badge.
### Revoke a badge
Get an earner's badge.
### Delete an issued badge
Deleting a badge will remove all information, statistics, and history related to that badge from Credly's server. This deletion process complies with the GDPR regulations regarding the deletion of a user's personal data. As this process can take some time to accomplish, an email will be sent to the organization's technical contact email when it has finished.
### Get badge assertion
Get an assertion. Assertions are representations of an awarded badge, used to share information about a badge belonging to one earner.
### Get badge class
Get a badge class. A class is a collection of information about the accomplishment recognized by the Open Badge. Many assertions may be created corresponding to one BadgeClass.
### Get OBI issuer
Get an OBI issuer. An issuer or profile is a collection of information that describes the entity or organization using Open Badges. Issuers must be represented as Profiles, and recipients, endorsers, or other entities may also be represented using this vocabulary. Each Profile that represents an Issuer may be referenced in many BadgeClasses that it has defined. Anyone can create and host an Issuer file to start issuing Open Badges.
### List organizations
List all organizations.
### Get an organization
Get an organization.
### Update an organization
Update an organization's publicly assessable information.
### Get an event
Get an event belonging to an organization.
### List all events
List all events belonging to an organization.
### List an organization's tokens
List an organization's authorization tokens.
### Get issuers
Retrieve the set of authorized issuers for an organization.
### Get grantors
Retrieve the list of organizations that have added an organization as an issuer.
### Deauthorize an Issuer
Remove an organization from the list of authorized issuers.

## Known Issues and Limitations
There are no known issues at this time.
