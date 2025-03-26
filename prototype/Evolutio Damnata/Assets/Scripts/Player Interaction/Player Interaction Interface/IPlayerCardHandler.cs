using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerCardHandler
{
    void HandlePlayerCardSelection(int index, EntityManager entityManager);
}
