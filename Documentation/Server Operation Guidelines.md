# Server Operation Guidelines

Throughout the development and operation of the FreeSO Server, we made a few decisions because of reasons that wouldn't be fully apparent to any players. We can't enforce any of these decisions now, but since I'd rather see the community succeed over crashing and burning, I can tell you why they are important.

Typically when someone started their own fork server during FreeSO's run, the _first_ thing they did was deliberately subvert a bunch of these guidelines because they viewed them as limiting factors. We put them in place for very good reason, which will be explained below.

## "Fair Use" and unproven legality primer

The Sims Online is clearly owned by EA, even if they don't sell or host the server for it, and The Sims IP as a whole is still very active. This might seem like a killer obstacle for a project like FreeSO, which aims to reproduce The Sims Online and allow players to experience it again, but it's a lot less straightforward than that.

FreeSO is software written to read the original game assets of TSO, and present them in a manner similar to the original TSO client. The project itself is entirely original and does not redistribute any assets from TSO or other sims games. By itself, it is not infringing, but it's also pretty useless.

The original TSO client has been available for decades via a "free trial" download that was public on EA's FTP. Due to the nature of the game, this free trial includes _all_ of the game assets that The Sims Online used, so essentially users can download the original TSO assets for free.

You can kind of see where I'm going with this - the entirely original FreeSO client, paired with the free TSO assets downloaded separately, can together _create_ a game that is very close to The Sims Online without actually distributing any of EA's assets. This constitutes fair use of the TSO assets, facilitated by the user, that should protect us from any claim of copyright violation and allow The Sims Online to live forever, as it should.

Projects like this are starting to become more common (more in the form of decompilations, rather than black box reimplementations like FreeSO), but it is still largely unproven in a court of law whether they are truly legal. I think it would be an easy win in FreeSO's case, but it won't be proven until someone gets angry enough to go to court about it, and that usually involves treading on toes enough for companies to build a case against you. 

In the past, **I was contacted by EA to make certain changes to the way we operate, and to ensure that we don't infringe on The Sims IP in ways not covered by software copyright.** I'm passing on this advice to hopefully avoid any future hosts of TSO servers encountering angrily worded emails, or potential a cease and desist that could come their way in future. Your actions might impact people's future ability to host a TSO server, so please consider the following carefully.

## DO NOT accept cash donations

FreeSO is unique as a fan project in that it provides access to an aspect of an intellectual property that is no longer available, rather than just adding our own content onto an existing playable title like The Sims 4. This means that we are effectively _replacing_ The Sims Online in its entirety, and giving users exclusive access to EA's lost IP.

This makes it easier to argue that we have the ability to _profit_ off of EA's intellectual property (The Sims Online), if we were to sell access to FreeSO. For that reason, it's important that you DO NOT accept cash donations for the operation of any part of the server. Especially _do not gate features, objects or provide rewards for real money_. Any profit you make from a cash donation becomes a liability, and server costs for FreeSO servers are small enough that it's likely you will exceed your costs with just a few contributors, making the spill over profit you've made from EA intellectual property.

Providing rewards for donations can very quickly equate to selling parts of TSO. _Do not do it!_

The route FreeSO has taken in the past is to accept donations of fixed assets, rather than liquid cash. People have provided servers, websites, forums - all so that it didn't have to be paid for by one person. This does mean that your donators need to be directly involved with the operation of the server, so you should be careful who you trust, but honestly splitting responsibilities can help you a lot in other ways, too. It does help that the typical number of players FreeSO has means that your server costs should not be too expensive - FreeSO's server was on $64 a month on DigitalOcean for the last few years (premium intel, 4 vCPUs, 8GB RAM) and it was honestly overspecced for most of that time.

## DO NOT distribute copyrighted assets

This might seem contradictory at first glance, though if you read the "fair use" section, you should know what's coming. The primary purpose of FreeSO is to fully replace the TSO game client and server. The game code is fully original - the _assets_ are sourced from an unmodified copy of The Sims Online that has been downloaded by the user as part of the installation process.

Files for "The Sims Online" are available for free on the internet via a trial installer that was hosted on an FTP server EA had running for a decade after the game's shutdown, and more recently from the Internet Archive. We don't _own_ these assets, we are _not_ redistributing them, we are pointing users to the game files as provided by EA themselves, and then our client reads them from your PC. This is a special distinction that keeps the FreeSO client entirely separate from any copyrighted material.

FreeSO has taken great care to avoid distributing even modified versions of TSO objects with the game client. The "Patch IFF" or `PIFF` format describes a list of changes to perform to an existing TSO iff to modify or add functionality in existing game assets without redistributing the whole thing. It only contains the difference between the original and target - so any added/removed chunks, and binary sections to replace and insert new data.

FreeSO's catalog was filled out by a lot of original objects from talented creators over the course of 8 years. This is a _lot_ harder to do than just using stuff other people have made for TS1, but it makes content that's a much better fit for the game, 100% legal, and arguably more satisfying for users to play with something unique that they haven't seen before.

Basically, the whole project has been built around this very specific protection of Fair Use. OK, so what kind of things should you avoid distributing?

### TS1 Objects

This is very tempting. The Sims Online has all of the base game objects from The Sims, and _most_ of the expansion objects up to Unleashed... so why _shouldn't_ it have all of TS1's objects? Well, because it _wasn't included with the TSO client_. It was not distributed for free on the internet, it was not downloaded by the user, so it's up to _YOU_ to obtain and distribute to users.

At this point, you've transitioned to distributing (hopefully modified) objects from The Sims Complete Collection, a paid software product that was never intended to be downloaded for free. This is no longer fair use, and you're up against a potential lawsuit from EA. You really want all the safety you can get.

Additionally, TSO is a hard fork from TS1, and it happens _very_ early. The layout of everything shared between objects (globals, semi-globals, primitives, scopes) changes dramatically around Hot Date/Vacation. You must manually patch these objects, or they will frequently error and generally work incorrectly. In certain cases, it would be easier to remake the object entirely, copying the old graphics over onto a TSO object base... but that wouldn't solve your copyright problem, it's more a consideration for the next category.

### TS1 Custom Content

The Sims 1 has a lot of custom content, and TSO is based off of the same technology base. So a lot of it works out of the box, right? Let's just lift it!

Well, no. As I described in the above section, a lot of things were changed in TSO - mostly starting around the Hot Date/Vacation expansions - that will introduce glaring issues with the object if you try to use it directly. The base for a lot of furniture objects - namely their "semi-globals" have also changed significantly. This allows similar objects like Chairs to share the same scripts, making it easier to add new functionality to existing chairs without changing them all.

Additionally, while TS1 CC creators might not have as much legal muscle as EA, you're not just downloading their stuff to play in your own game. You're distributing it with your modified game client, to _everyone_. You should ask the creator for permission to do this - they might not want you to claim their old CC as your own, especially if you're accepting donations for it (which you shouldn't be doing anyways). Getting approval will take a load off your mind as well, and telling players you have permission will dispel doubts that you're just putting things in there willy nilly.

### Other games

Importing assets directly from other games mostly looks shoddy, but follows a lot of the same rules - it just changes who is going to serve you a court order. If you import furniture from Animal Crossing, you'll have Nintendo to answer to. If it's from GTA, then it's Rockstar. Maybe you'll find some furniture to rip from someone who doesn't have offices full of IP lawyers ready to strike, but I suggest you just stick to making your own _similar_ items of furniture, rather than just ripping things directly, so that you don't have to Cease and Desist operation of your server, leaving all your users stranded without warning.

FreeSO has been careful in this regard too, with custom crafted assets _referencing_ other IP that are clearly meant for parody purposes, not just clear rips. Even integration of a ported version of libsm64 for April Fools was done with a lot of care - no copyrighted asset was distributed with the client. Please do be careful.

## Code modifications should be Open Source

FreeSO is under the MPL v2.0 license, which means that any distribution of the FreeSO executable or content (including modified versions or use as a dependency) MUST be accompanied by a source code release that matches. See 3.2(a). Please read the full `LICENSE.md` in the root directory for more information. License violations are **legally actionable**, but it's probably better to just convince you on why it makes sense for this project.

FreeSO has always been open source for a few reasons, even back when it was Project Dollhouse, mostly relating to the history of TSO revival projects:

- Mismanaged and Scam projects meant that trust for any TSO related projects was very low. One such project disappeared with thousands of dollars of people's money.
- Making an open source project meant that any skeptic could build the project for themselves and see it working. This also attracted developers that wouldn't have been involved otherwise... like myself.
- Frequent updates and test branches/builds reinforced that even when the game wasn't particularly playable, people could see progress and believe in the project's future.
- Even if the developers of the project disappear, the existing work can be picked up and continued by someone else in the future - it never truly dies.
- Developers, out of interest or skepticism, could examine the code for themselves to make sure nothing misleading or unsavoury was going on, and notify the community if there was something that didn't add up.

Now that the majority of the work is "done", you could say that a lot of these things are no longer an issue. We proved that TSO could be revived, and you can play it right now. However, it's still important to keep the code open source, as there are a lot of players counting on the code being audited by developers to ensure they aren't downloading malware, and there are a lot of skilled developers whose first appearance will be a code contribution rather than integrating directly into your team. The future of TSO also relies on public contributions, as improvements made to a private fork are improvements that will be lost forever if your server is to disappear for some reason.

While you could see keeping your code closed source as a way to push changes that may have flaws without them being criticised, the existence of these flaws could put your playerbase at risk via client vulnerabilities, or cause problems for yourself later on that could have been solved by a contributor outside your core team. It's just much easier to throw it on GitHub.

Ideally, your builds should also be performed by public CI actions on GitHub Actions or Azure. This makes it easier for you to deploy updates, but it also makes it easier for users to see that the updates you're distributing are built from the code they see in the repository, creating more user trust. This also fulfils 3.2(a) of the MPL v2.0 license, as your distributed code will always match publicly available code.

## Multi-accounting and player equality

A big problem in TSO's history has been players running multiple accounts at the same time. Players would open the game client multiple times to play as multiple sims at the same time, allowing them to earn 3x as much skill and money as a normal player for one account. They would join skill and money lots, take up three slots with their own sims and flip between windows, talking using whichever of the three windows they had opened at the time, meaning their sims would display some sort of hivemind to any outside observer. They would also play the multiplayer games with themselves... which seems it's opposed with the conceit of TSO being a "social" game, but people did it regardless.

If someone is running more than one sim to earn money or skills, that leaves anyone playing the game with one sim at a time at a major disadvantage. The game quickly becomes about how many client windows you can keep open and flip between without your graphics driver crashing - which you can imagine is a lot less engaging from a gameplay perspective. For anyone more interested in on the social interaction aspect of the game and their relationships with individual sims, a world filled with hivemind triplets greatly discourages that kind of play and quickly devolves the game into a grinding simulator.

This wasn't a problem for EA to solve - it actually benefited them. During the original run of TSO, playing the game required a subscription of $10 a month per account, so they would actually pick up more money when people doubled or tripled down on more accounts. The damage it did to the game back then is hard to prove, but these days you could view this as "pay-to-win" gameplay, where more subscriptions would net players more of an advantage over their peers. You can see why EA did not attempt to solve this problem, but that doesn't mean we have to do the same.

A lot of arguments against forcing everyone into playing one sim is that it's not engaging enough for certain people - but this mostly ignores the technical hurdles with maintaining multiple game clients. A past argument has been that one client could be allowed to play multiple sims at the same time, with sim switching similar to TS1, avoiding opening multiple. However - this just makes it much easier to play more sims, so you'll have _8 clones_ of everyone running around instead of 2 or 3. Implement a manual limit? People will just open another client to get more sims again; the problem wasn't solved and you have more duplicates.

You could call this a major flaw in the original game's design, and you would be right. There are a lot of those, feel free to take a crack at them if you want multiaccounting to break the game less. Our solution was to track multiaccounters using the last logged IP and client ID hash. These are typically used for password attempt rate limiting, but they can be hijacked for some rudimentary multiaccount tracking. Do whatever you want to try avoid these problems.

## Starting Funds

It's The Sims tradition for people to start with $20,000 simoleons when they create a family. I'm here to tell you that for The Sims Online, at least when people can create accounts for free (which they should be), this is The Worst Mistake You Can Make.

Early in FreeSO's server hosting, chaos reigned and we had a $20,000 simoleon bonus for players who joined the game. This immediately caused a few players to create 6 accounts, with 3 sims on each, earning a total of $360,000 instantly. These people were banned forever and someone threatened to report us to Vanity Fair. We are still waiting for the article.

It is true that removing starting funds can make it hard to earn any skills or money, as nobody will be able to afford property. I'd suggest running a town hall as an admin to get people started, then you can remove the training wheels when real money and skill lots start to crop up. This shouldn't take too long.

## Speeding up Skilling or Money dramatically

FreeSO _already_ increased the speed of skilling and money making from the snails pace it had in TSO, so that player goals actually felt attainable. In some ways, this was already too much for the original game's design - there weren't enough money sinks or skill levels for end-game players and players ran out of things to do instead of "playing forever".

Personally, I would argue that this is a good thing... You should be able to achieve your goals and move on, but this causes a problem where you have an _outflow_ of players that needs to be matched or exceeded by an _inflow_. There may be some evergreen players, but the reality is that your player count will dwindle and the playability of the game will be impacted with it. Player presence in the game is very important in TSO as they host lots and fill out social spaced, so while people's time with the game will be limited, you want it to be long enough that the players actually see each other before disappearing into the sunset.

A sort of balance was maintained while we were introducing new objects and gameplay before 2020, as players return or continue playing for longer if you introduce new gameplay for them to experience. However, it faltered when I stopped working on features, and entirely _broke_ when regular events halted.

If you were to 5x skill and money speed, there would never be any balance to maintain. People will blast through the game in 2 months and you'll have nobody left in a year. You won't get evergreen players because there won't be anyone for them to talk to. 

## Private Server Moderation

This section is a lot less clear cut over any of the others. I'm asking you to properly consider who you're putting in charge of your server. If run properly, people will spend a large chunk of their lives building relationships and achieving goals on your server, and it's very important that those things aren't destroyed due to mismanagement or rogue moderation.

For starters, if someone was banned from other sims communities and wants a position of power on your server, consider that there was likely a reason they were banned from those communities. On a more complicated note, you should personally vet potential moderators over a long period of time to make sure they don't pick sides in community feuds and harass other players. Making a person like that a moderator will likely split the community, so make sure you pick people who are impartial.

This might seem like obvious advice, but it's really easy to make this mistake if you aren't paying enough attention to your community. It is very common to see this happen in any hobbyist community - _especially_ mmo private servers - so don't get rushed into making people moderators.

## Keep it lighthearted

Nothing lasts forever, so might as well have fun while you can. Find time to enjoy yourself in the space you created, and it'll be better as a result. Don't overwork yourself.
