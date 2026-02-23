using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using System.Linq;

public class SlotMachine : View
{
    [Space]
    [Header("Multi-Reel Setup")]
    [SerializeField] private RectTransform[] itemsContainer;
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private UtilityDatabase utilityDatabase;
    
    [Space]
    [Header("Content")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private RectTransform maskArea;

    [Space]
    [Header("Layout")]
    [Tooltip("Slots total que generara, cuantos mas haya, mas rapido girara ya que el Slot final (resultado) siempre esta por el final.")]
    [SerializeField] private int totalSlots = 50;
    [Tooltip("Espacio que hay entre slot y slot.")]
    [SerializeField] private float slotSpacing = 250f;
    [Tooltip("Altura en la que empiezan a spanwear los slots.")]
    [SerializeField] private float spawnHeight = 300f;

    [Space]
    [Header("Spin")]
    [Tooltip("Segundos que dura el spin.")]
    [SerializeField] private float spinDuration = 5f;
    [Tooltip("Tipo de curva que seguira el spin.")]
    [SerializeField] private AnimationCurve spinCurve;
    [Tooltip("Decide de que tipo se hara el random.")]

    [Space] [Header("Audios")]
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private int reelsFinished = 0;
    public bool allFinished = false;
    public WeaponScripteableObject finalWeapon;
    private int roundsCountInLastSpin;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        //InstanceHandler.UnregisterInstance<SlotMachine>();
    }

    public void startMultiSpinFlat(int[] winners, int[] p1, int[] p2, int[] p3)
    {
        StopAllCoroutines();
        reelsFinished = 0;
        allFinished = false;
        
        roundsCountInLastSpin = 0;
        foreach(var w in winners) if(w != -1) roundsCountInLastSpin++;

        List<int[]> pools = new List<int[]> { p1, p2, p3 };

        for (int i = 0; i < winners.Length; i++)
        {
            if (i >= itemsContainer.Length) break;

            itemsContainer[i].gameObject.SetActive(true);

            if (winners[i] == -1) 
            {
                // Apagamos la Mask/Columna entera para que no se vea el hueco vacío
                itemsContainer[i].gameObject.SetActive(false);
                continue;
            }

            Sprite winnerIcon;
            List<Sprite> poolIcons;

            if (i == 2) // LA TERCERA COLUMNA: UTILIDADES
            {
                var utility = utilityDatabase.GetUtilityByID(winners[i]);
                winnerIcon = utility.icon;
                poolIcons = pools[i].Select(id => utilityDatabase.GetUtilityByID(id).icon).ToList();
            }
            else // COLUMNAS 1 Y 2: ARMAS
            {
                var weapon = weaponDatabase.GetWeaponByID(winners[i]);
                winnerIcon = weapon.icon;
                poolIcons = pools[i].Select(id => weaponDatabase.GetWeaponByID(id).icon).ToList();
            }

            // Lanzamos la rutina pasándole solo los Sprites, así no importa de qué DB vengan
            StartCoroutine(SpinRoutineMulti(i, winnerIcon, poolIcons));
        }
    }


    private IEnumerator SpinRoutineMulti(int index, Sprite winnerIcon, List<Sprite> poolIcons)
    {
        RectTransform container = itemsContainer[index];

        // 1. Limpieza
        foreach (Transform child in container) Destroy(child.gameObject);
        List<RectTransform> currentSlotList = new List<RectTransform>();

        // 2. Creación de iconos
        int winnerIndex = Random.Range(totalSlots - 14, totalSlots - 5);

        for (int i = 0; i < totalSlots; i++)
        {
            Sprite s = (i == winnerIndex) ? winnerIcon : poolIcons[Random.Range(0, poolIcons.Count)];
            
            var slot = Instantiate(slotPrefab, container).GetComponent<RectTransform>();
            slot.GetComponent<Image>().sprite = s;
            slot.anchoredPosition = new Vector2(0, spawnHeight - (-i * slotSpacing));
            currentSlotList.Add(slot);
        }

        // 3. Posicionamiento y Animación
        float desiredContainerY = maskArea.anchoredPosition.y - currentSlotList[winnerIndex].anchoredPosition.y;
        Vector2 startPos = new Vector2(container.anchoredPosition.x, 0);
        Vector2 endPos = new Vector2(container.anchoredPosition.x, desiredContainerY);

        yield return new WaitForSeconds(0.3f);

        float elapsed = 0f;
        float lastY = startPos.y;
        float distanceAccumulator = 0f;

        while (elapsed < spinDuration)
        {
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float curveT = spinCurve != null ? spinCurve.Evaluate(t) : t;
            container.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);

            float deltaMove = Mathf.Abs(container.anchoredPosition.y - lastY);
            distanceAccumulator += deltaMove;

            if (distanceAccumulator >= slotSpacing)
            {
                PlayTickSound();
                distanceAccumulator = 0;
            }

            lastY = container.anchoredPosition.y;
            elapsed += Time.deltaTime;
            yield return null;
        }

        container.anchoredPosition = endPos;
        
        // 4. Control de finalización
        reelsFinished++;
        if (reelsFinished >= roundsCountInLastSpin) 
        {
            allFinished = true;
        }
    }

    private void PlayTickSound()
    {
        if (tickSound != null && AudioManager.Instance != null)
        {
            float randomPitch = Random.Range(minPitch, maxPitch);

            AudioManager.Instance.PlaySound2D(
                tickSound,
                AudioType.UI, 
                .25f,            // Volumen
                randomPitch    // Pitch
            );
        }
    }

    // Elegir un arma segun sus posibilidades
    //private WeaponScripteableObject ChooseWeaponByChance(List<WeaponScripteableObject> weaponList)
    //{
    //    float totalChance = 0f;
    //    foreach (var w in weaponList)
    //        totalChance += w.dropChance;
//
    //    float r = Random.Range(0f, totalChance);
    //    float accum = 0f;
//
    //    foreach (var w in weaponList)
    //    {
    //        accum += w.dropChance;
    //        if (r <= accum)
    //            return w;
    //    }
//
    //    return weaponList[weaponList.Count - 1];
    //}

    

    public void Skip()
    {
        spinDuration = spinDuration / 5;
    }
    

    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }




}
