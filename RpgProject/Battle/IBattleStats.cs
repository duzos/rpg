using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Battle;

public interface IBattleStats {
    int Health { get; set; }
    int MaxHealth { get; set; }
    int Attack { get; set; }
    void OnBattleStart(BattleState state) { }
    void OnBattleEnd(BattleState state) { }
}