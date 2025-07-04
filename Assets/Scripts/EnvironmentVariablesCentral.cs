using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;


public static class EnvironmentVariablesCentral
{
    public static string clientExeDir;
    public static string gameDir;
    public static string serverDir;
    public static string compiledServerDir;
    public static string saveDir;
    private static string invisScript = "start powershell start-process $env:APPDATA\\DraconicRevolution\\Server\\Server.exe -WindowStyle hidden -ArgumentList '-Local'";


    public static void Start(){
        clientExeDir = GetClientDir();
        gameDir = GetAppdataDir() + "\\DraconicRevolution\\";
        serverDir = gameDir + "Server\\";
        saveDir = EnvironmentVariablesCentral.serverDir + "Worlds\\";

        #if UNITY_EDITOR
            compiledServerDir = clientExeDir + "Build\\Server";
        #else
            compiledServerDir = GetParent(clientExeDir) + "\\Server";
        #endif

        if(!Directory.Exists(gameDir))
            Directory.CreateDirectory(gameDir);

        // Moves compiled server to APPDATA
        if(Directory.Exists(compiledServerDir)){
            if(Directory.Exists(serverDir)){
                if(Directory.Exists(saveDir)){
                    Directory.Move(saveDir, gameDir + "Worlds\\");
                }

                Directory.Delete(serverDir, true);
            }

            Directory.Move(compiledServerDir, serverDir);

            if(Directory.Exists(gameDir + "Worlds\\")){
                Directory.Move(gameDir + "Worlds\\", saveDir);
            }
        }

        if(!Directory.Exists(serverDir)){
            if(Directory.Exists(compiledServerDir)){
                Directory.Move(compiledServerDir, serverDir);
            }
            else{
                Application.Quit();
            }
        }
    }

    public static void StartServer(){
        clientExeDir = GetClientDir();
        gameDir = GetAppdataDir() + "\\DraconicRevolution\\";
        serverDir = gameDir + "Server\\";
        saveDir = EnvironmentVariablesCentral.serverDir + "Worlds\\";
    }

    public static void WriteInvisLaunchScript(string worldName){
        string dump;

        if(worldName == ""){
            dump = $"{invisScript}";
        }
        else{
            dump = $"{invisScript}, '-World', '{worldName}'";
        }

        byte[] bytes = Encoding.ASCII.GetBytes(dump);
        Stream invisFile = File.Open(serverDir + "invisLaunchHelper.bat", FileMode.Create);
        invisFile.Write(bytes, 0, bytes.Length);
        invisFile.Close();
    }

    private static string GetAppdataDir(){
        return GetParent(Application.persistentDataPath, iterations:3) + "\\Roaming";
    }

    public static List<string> ListFilesInWorldFolder(string worldName, string extensionFilter="", char firstLetterFilter='\0', bool onlyName=false){
        string worldDir = saveDir + worldName + "/";
        List<string> fileList = new List<string>();

        string[] files = Directory.GetFiles(worldDir);
        string[] splitted;
        string fileName = "";

        foreach (string file in files){
            if(extensionFilter != ""){
                if(file.Split(".")[1] != extensionFilter)
                    continue;
            }

            splitted = file.Split("/");

            if(onlyName)
                fileName = splitted[splitted.Length - 1].Split(".")[0];
            else
                fileName = splitted[splitted.Length - 1];

            if(firstLetterFilter != '\0'){
                if(fileName[0] != firstLetterFilter)
                    continue;
            }

            fileList.Add(fileName);
        }

        return fileList;
    }

    public static List<string> ListWorldFolders(){
        List<string> directories = new List<string>();

        if(!Directory.Exists(saveDir)){
            Directory.CreateDirectory(saveDir);
        }

        string[] dirArray =  Directory.GetDirectories(saveDir);

        foreach(string dir in dirArray){
            directories.Add(dir);
        }

        return directories;
    }

    public static void PrintDirectories(){
        string a = "";
        a += ("DataPath: " + Application.dataPath + "\n");
        a += ("clientExeDir: " + EnvironmentVariablesCentral.clientExeDir + "\n");
        a += ("gameDir: " + EnvironmentVariablesCentral.gameDir + "\n");
        a += ("serverDir: " + EnvironmentVariablesCentral.serverDir + "\n");
        a += ("saveDir: " + EnvironmentVariablesCentral.saveDir + "\n");
        a += ("compiledServerDir: " + EnvironmentVariablesCentral.compiledServerDir);

        File.WriteAllText("Directories.txt", a); 
    }

    private static string GetClientDir(){
        return GetParent(Application.dataPath, iterations:1) + "\\";
    }

    private static string GetParent(string path, int iterations=2){
        string newPath = path;

        while(iterations > 0){
            newPath = Directory.GetParent(newPath).ToString();
            iterations--;
        }

        return newPath;
    }
}
