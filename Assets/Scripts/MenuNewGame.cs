using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Text;
using TMPro;

public class MenuNewGame : MonoBehaviour
{
    public TMP_InputField seed;
    public TMP_Dropdown difficulty;
    public TMP_InputField save;

    const string glyphs= "abcdefghijklmnopqrstuvwxyz0123456789";
    static string RandString(int length)
    {
        StringBuilder sb = new StringBuilder();
        for(int i=0; i<length; i++)
        {
            sb.Append(glyphs[UnityEngine.Random.Range(0, glyphs.Length)]);
        }
        return sb.ToString();
    }

    public void CapSeed() {
        string value = this.seed.text;
        if(!long.TryParse(value, out long seed)) {
            if(value.Length <= 1) return;
            if(value[0] == '-') {
                this.seed.text = long.MinValue.ToString();
            } else {
                this.seed.text = long.MaxValue.ToString();
            }
        }
    }

    public void StartGame()
    {
        if(!long.TryParse(seed.text, out ProceduralGeneration.seed))
        {
            if(seed.text == "-") {
                ProceduralGeneration.seed = -1;
            } else {
                ProceduralGeneration.RandomSeed();
            }
        }

        EnemySpawning.difficulty = difficulty.value;

        SaveData.currentSaveName = save.text;
        if(SaveData.currentSaveName == "") SaveData.currentSaveName = save.placeholder.GetComponent<TextMeshProUGUI>().text;

        int i=0;
        do
        {
            SaveData.currentSaveFileName = RandString(10)+".save";
            i++;
        }
        while(File.Exists(SaveData.DIRECTORY_PATH + SaveData.currentSaveFileName) && i < 1000);

        EnemySpawning.instance.Reset();
        PlayerMovement.instance.Reset();
        PlayerInventory.Reset();
        // other reset shit

        MenuHandler.StartGame();
    }
}