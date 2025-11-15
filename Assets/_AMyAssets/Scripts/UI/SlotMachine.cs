using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;

public class SlotMachine : View
{
    [Space]
    [Header("Content")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private RectTransform itemsContainer;
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
    private List<RectTransform> slotList = new List<RectTransform>();
    private bool isSpinning = false;
    public WeaponScripteableObject finalWeapon;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<SlotMachine>();
    }


    public void startSpin(WeaponScripteableObject selectedWeapon, List<WeaponScripteableObject> filteredWeapons)
    {
        StartCoroutine(Spin(selectedWeapon, filteredWeapons));
    }

    public IEnumerator Spin(WeaponScripteableObject selectedWeapon, List<WeaponScripteableObject> filteredWeapons)
    {
        if (isSpinning) yield break;
        itemsContainer.anchoredPosition = Vector2.zero;
        yield return StartCoroutine(SpinRoutine(selectedWeapon, filteredWeapons));
    }


    private IEnumerator SpinRoutine(WeaponScripteableObject selectedWeapon, List<WeaponScripteableObject> filteredWeapons)
    {
        isSpinning = true;


        // Borramos todos los iconos anteriores
        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);
        slotList.Clear();

        // Creamos los nuevos iconos
        int winnerIndex = Random.Range(totalSlots - 14, totalSlots - 5);

        for (int i = 0; i < totalSlots; i++)
        {
            // Si es el indice del ganador -> usar el arma ganadora
            var w = (i == winnerIndex)
            ? selectedWeapon
            : filteredWeapons[Random.Range(0, filteredWeapons.Count)];

            // Crear el icono en la UI
            var slot = Instantiate(slotPrefab, itemsContainer).GetComponent<RectTransform>();
            slot.GetComponent<Image>().sprite = w.icon;

            // Colocar el icono uno debajo del otro
            slot.anchoredPosition = new Vector2(0, spawnHeight - (-i * slotSpacing));
            slotList.Add(slot);
        }

        // Calcular a donda mover el contenedor
        // El objetivo es que el icono ganador termine justo en el centro de la mascara
        RectTransform winnerRect = slotList[winnerIndex];
        float maskCenterY = maskArea.anchoredPosition.y;
        float winnerLocalY = winnerRect.anchoredPosition.y;
        float desiredContainerY = maskCenterY - winnerLocalY;

        // Guardamos posiciones de inicio y final
        Vector2 startPos = itemsContainer.anchoredPosition;
        Vector2 endPos = new Vector2(itemsContainer.anchoredPosition.x, desiredContainerY);

        // Animar el movimiento con la curva
        yield return new WaitForSeconds(0.3f);
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float curveT = spinCurve != null ? spinCurve.Evaluate(t) : t; // Valor modificado por la curva
            itemsContainer.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Asegurar que se qeuda exactamente donde debe
        itemsContainer.anchoredPosition = endPos;

        finalWeapon = selectedWeapon;
        Debug.Log($"🎯 Ganador: {selectedWeapon.weaponName}(intex {winnerIndex})");
        isSpinning = false;
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
