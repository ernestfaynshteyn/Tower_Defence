using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public GameObject[] guns;

    public void IncreaseDamageByPercen(float increase)
    {
        for (int i =0; i < guns.Length; i++)
        {
            if (guns[i].GetComponent<Shottingscript>()!=null)
            {
                Shottingscript sc = guns[i].GetComponent<Shottingscript>();
                sc.SetDamage(sc.GetDamage() * increase);
            }
        }
    }
    public void IncreaseDamage(float increase)
    {
        for (int i = 0; i < guns.Length; i++)
        {
            if (guns[i].GetComponent<Shottingscript>() != null)
            {
                Shottingscript sc = guns[i].GetComponent<Shottingscript>();
                sc.SetDamage(sc.GetDamage() + increase);
            }
        }
    }
}
