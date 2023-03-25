## Хендлер доты 2, позволяющий взаимодействовать с серверами игры.

Пример использования есть в проекте Example.

Хендлер сам будет пытаться заходить в доту, пока `SteamClient` онлайн.

### Колбеки, связанные с работой хендлера.

`DotaReadyCallback` Подключение к серверам доты завершено.

`DotaNotReadyCallback` Подключение к серверам доты потеряно.

`DotaHelloTimeoutCallback` Не удаётся установить подключение с серверами игры. В таком случае возможно стоит перезапустить `SteamClient`.

### Доступные операции.

`RequestRichPresence` - Запрос информации RichPresence игроков по SteamId64.
Возвращает массив байт и SteamId64 игрока. Для получения информации из полученных байт есть класс `DotaRichPresenceInfo`.

`RequestSpecificSourceTvGames` - Запрос информации о матчах по LobbyId.

`RequestSpectateFriendGame` - Запрос информации об игровом сервере игрока по SteamId64.

LobbyId можно получить из `DotaRichPresenceInfo` (watchableGameId).

## Api
Также есть класс `DotaApi`, позволяющий обращаться к апи доты.

`GetMatchDetails` - Узнать информацию о завершённом матче по MatchId.

MatchId можно получить из `SourceTvGamesCallback`.

# Благодарности

Благодарю разработчиков 9kmmrbot, их бот вдохновил меня на создание этой библиотеки.
https://github.com/Hambergo/9kmmrbot