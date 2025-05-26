using System.Collections.Generic;
using UnityEngine;

public interface IGraveYard
{
    void AddToGraveyard(EntityManager entity, EntityManager killedBy, float damage);
    void AddSpellKill(EntityManager entity, string spellName, float damage, bool isOngoingEffect = false);
    List<GraveYard.DeadEntityRecord> GetGraveyardHistory();
    void ClearGraveyard();
    bool KeepHistoryBetweenGames { get; set; }
    int MaxHistorySize { get; set; }
} 