# SmileDentistBackend

![MicrosoftSQLServer](https://img.shields.io/badge/Microsoft%20SQL%20Sever-CC2927?style=for-the-badge&logo=microsoft%20sql%20server&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)

## Table Of Contents
* [General Info](#general-info)
* [Setup](#setup)

## General Info
This Project is the backend portion for "smiledentistfrontend"

## Setup
#### Sql Server:

Make sure you've Sql server installed on your platform. It can be found here: [Microsoft Sql Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

#### SendGrid:

I'm using sendgrid to process the emails messages. To make sure the api key wont get blocked I'm using environment variables for several places. 
They are located in "AuthController" for the emails, if you want to use a different email you can just hard code it. The variable to look for is emailSettings in there.

In "SendGridEmailRegister", "SendGridEmailBookings" and in "SendGridEmailTokens" please exchange the variable environmentVariableKey with your api key. I'm also using customized Templates for sendGrid. If you want to use a SendGridTemplate please create two new in sendgrid and provide them with the variables from SetTemplateData.

In the sendGridTemplates please provide a variable formated: {{SetTamplateData Variable}}

#### Visual Studio:

To run the project yuu need an IDE, like Visual Studio Community: [Visual Studio](https://visualstudio.microsoft.com/vs/community/)

#### Update The Database:

To update the database simply use the Package Manager Console that comes integrated with visual studio community and simply write "Update-Database"

#### Quartz

Please note that the schedulerer is designed to only run between 06 and 17 local time and no booked messages will be sent outside that timeframe. If you want to use it outside that you have to modify ScheduledHostedService class and make modifications to the frontend.
