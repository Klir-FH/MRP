Github: https://github.com/Klir-FH/MRP.git
Postman Collection Link: https://if25b233-2699819.postman.co/workspace/if25b233's-Workspace~f382fadd-582f-4898-862e-b27b5a3d9999/collection/49029395-c1bfbfbf-0b83-44c4-9cc8-eea105a9428b?action=share&creator=49029395
Ive also exported the Postman Collection as a JSON.

Before starting run "docker compose up -d"  to setup the db.

App Design:

The solution follows a 3 tier Design.

MRP-DAL represents the Data Access Layer handling all Database interactions.
MRP-Server implements the Business Logic 
There is no Presentation Layer as its not in the requirements. 
My third tier is the Models representing all DTOS and entities using in communication.

Ive decided on using a JWT based Authentication while also utilizing RSA encryption because i liked the challenge.

Credentials to User is a 1:n relationship. This is for a possible future addition of MFA.

Added JAB for compile time DI.

I generally use most of the Models as representations of the DB. Due to the low complexity, data is mostly written directly into the db without being parsed into the corresponding Model.

