# Напоминалка по статусам в RP.

Статус есть всегда, в зависимости от статуса, приходят дополнительные поля param0 param1 и т.д.

Статусы и их параметры можно узнать из протобафа ServiceMethodResponse (147), который приходит при запуске доты (их таких два приходит, нужен второй)

`RpStatusHelper` содержит немного помощи.

## Статусов много, отмечу важные для меня.

`#DOTA_RP_FINDING_MATCH` Finding A Match

выбор героя
`#DOTA_RP_HERO_SELECTION` {%param0%}: Hero Selection
Первый параметр - тип лобби. #demo_hero_mode_name

после пиков
`"#DOTA_RP_STRATEGY_TIME"` {%param0%}: Strategy Time
Первый параметр - тип лобби. #demo_hero_mode_name

игра
`#DOTA_RP_PLAYING_AS` {%param0%}: Lvl %param1% {%param2%}
Первый параметр - тип лобби. #demo_hero_mode_name
Второй параметр - уровень героя. 30
Третий параметр - герой. #npc_dota_hero_lich

## Важные типы лобби
#DOTA_lobby_type_name_unranked
#DOTA_lobby_type_name_ranked
#game_mode_23 //Это турбо

# Тут нужно уточнить
Есть ещё куча других статусов, всякие нюансы есть, например, там разные PLAYING_AS, есть турнирные, лобби всякие. Мне это не нужно, но это может быть.