# Registre de canvis

Totes les versions de **Quina Barra!**, de la més nova a la més antiga.

## [1.1] · 24/11/2026

Ara parla nou llengües.

- **Llengües noves:** valencià, gallec, basc, asturià, aragonès, occità, aranès i alguerès, a més del català. Traduïdes amb l'ajuda de Claude i revisades amb la norma de cada llengua; no som lingüistes, així que els suggeriments són benvinguts.
- El giny agafa la llengua del Windows si la té; si no, surt en català. Es pot canviar a la pàgina nova **Configuració** (roda dentada del menú ⋯), i es desa a la configuració.
- Els llocs desats s'endrecen **arrossegant les pastilles**; la primera és la principal, la de la barra.
- **Endreça el panell** (a sota de tot del panell): les targetes s'arrosseguen al panell mateix; a Configuració, les tres ratlles fan el mateix des de la llista.
- **Configuració** substitueix «Personalitza el panell» al menú ⋯ i al peu del panell: hi ha la llengua i, a sota, els blocs del panell.
- Els textos ja no són dins del codi: cada llengua és un fitxer a `src/textos/` (format jquery.i18n), amb una nota per a cada text que explica on surt.
- `/prova <carpeta> <llengua>` fa les captures en la llengua que triïs i avisa si falta algun text.
- A part de la pàgina Configuració, en català el giny es veu i funciona igual que la 1.0.

## [1.0] · 23/11/2026

Primera versió publicada.

- Giny a l'esquerra de la barra de tasques de Windows 11, amb la icona del temps, la temperatura i l'estat del cel.
- Diversos llocs desats en pastilles; el principal és el de la barra. Cercador de pobles, ciutats i barris, amb els noms oficials de l'Idescat per als municipis de Catalunya.
- Temperatura com a mitjana de tres models (Météo-France, ICON i ECMWF).
- Resum «Què ens espera», pròximes 24 hores i pròxims 10 dies.
- Rajoles de precipitació, vent, sortida i posta del sol, índex UV, qualitat de l'aire (ICA), visibilitat, humitat i pressió, amb pàgina de detall per a cadascuna.
- Targeta «Mar» als llocs de costa, amb l'estat de les platges.
- Radar de pluja animat sobre OpenStreetMap.
- «Personalitza el panell»: blocs visibles i ordre.
- Mode clar, fosc o com el Windows; «Engega amb el Windows»; avís de versions noves.
