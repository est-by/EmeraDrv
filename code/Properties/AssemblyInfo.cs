﻿using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyDescription("GranElectro")]

[assembly: AssemblyVersionAttribute("1.0.0.0")]
[assembly: InternalsVisibleTo("Gran101Drv, PublicKey=00240000048000009400000006020000002400005253413100040000010001001dc84667d77da42f5f580639c8dd6b8a964cf79cb2fe4d3e6977ac01be2cc0b7f06f2df552d3495c9a321c01f12d317284a8da6197441b913de786bd7cf3e6a427e12d05542f45e818af34cf13c6d94f75d971a5a0e2e5df8d5ea76b4726e11b69eabc4ec7b01b1bb1cf3ed6d7df4a181e1e1208412dcb58dbc72ca13fe0b7c4")]

[assembly: InternalsVisibleTo("GranDrv.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b55fda8cb088d19ff48465d45787c312925ca1b193f089cb44e93a4ad1f9ed7afb33e2a45822bf2b834f30cb33ded4978fe0594e82ebdf935efed09f9d281fde45bd79b1326224ad50079a2ae53aa9144479196d3ea389f235510ed416d55a44b4f0c5aa6900af151d8a5ef4c722726ef62239ac6bc5960c102952ef8a4039da")]

//1 - Добавлена подержка кэфициетов с плавующей запятой парметр 52, и увеличен архив 3 минутной мощьности
//2 - Добавлена подержка уровня доступа. Теперь если ввести пароль то счетчик будет всегда корректироваться      
