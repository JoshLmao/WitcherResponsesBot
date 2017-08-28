# Witcher Responses Bot

A reddit bot that can reply to comments in /r/Gwent & /r/Witcher subreddits with a link to the actual voice line. Inspired by [Jonarzz's DotaResponsesBot](https://github.com/Jonarzz/DotaResponsesRedditBot)

![Formatted Example](http://i.imgur.com/qCNHFNg.png)

The bot will only reply to comments that are meant for a reply from this bot. There is also margin for error like not having the correct punctuation

![MarginOfError Example](http://i.imgur.com/sNmfReF.png)

If there is multiple responses with the same content, the bot will try to use the one related to the users flair.

******

# How It Works (Internal Overview)

The bot works by first creating a database of all of the uploaded sound files from [gwent.gamepedia.com/Category:Audio](https://gwent.gamepedia.com/Category:Audio). It then saves the parsed data to a JSON file (if a filepath has been specified) so as to not rebuild it's database everytime.

The bot then gets the latest X amount of posts from 'New' & 'Hot' in any subreddits it is listening to and checks every comment for a match. At the moment, the bot will remove certain punctuation & certain Markdown formatting (like **bold** & *italics* but not ~~strikethrough~~) to make a valid check. Once a check has decided it should respond, it then formats a reply and posts it

* For command line parsing, I'm using [Fluent Command Line Parser](https://github.com/fclp/fluent-command-line-parser)
* For Reddit querying, I'm using [RedditSharp](https://github.com/CrustyJew/RedditSharp)
