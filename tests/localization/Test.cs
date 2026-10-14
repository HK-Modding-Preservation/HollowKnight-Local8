using System;
using System.IO;
using System.Reflection;
using KO.HollowKnight8;
static class Test {
 static int checks; static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  string[] languages={"ES","EN","FR","DE","IT","PT","RU","ZH","JA","KO"};
  using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Local8.UI.Text"))){
   string line;while((line=reader.ReadLine())!=null){if(line.Length==0||line[0]=='#')continue;var cells=line.Split('\t');
    Check(cells.Length==11,"Incomplete translation row "+cells[0]);
    string key=cells[0].Substring(3);
    for(int i=0;i<10;i++){
     Language.Language.Code=languages[i];
     Check(!string.IsNullOrWhiteSpace(cells[i+1]),"Empty translation "+key+" "+languages[i]);
     Check(Charms.SanitizeLocalized(key,"LOCAL8","SPANISH FALLBACK") == cells[i+1],"Actual resolver bypasses resource: "+key+" "+languages[i]);
     Check(Charms.SanitizeLocalized(key,"UI","native text") == "native text","Mod translation overwrites native UI");
     Check(Charms.SanitizeLocalized("unknown.key","LOCAL8","unknown fallback")=="unknown fallback","Unknown key loses legacy fallback");
    }
    Language.Language.Code="unsupported";
    Check(Charms.SanitizeLocalized(key,"LOCAL8","SPANISH FALLBACK")==cells[2],"Unsupported language does not fall back to EN");
   }
  }
  foreach(string lang in languages){Language.Language.Code=lang;
   Check(Charms.SanitizeLocalized("CHARM_DESC_1","UI","Daño") == "Dano","Charm glyph compatibility was lost");
   Check(Charms.SanitizeLocalized("DIALOGUE","Titles","Daño") == "Daño","Unrelated native dialogue altered");
   foreach(var role in RoleCatalog.All){Check(!role.Name.EndsWith(".name")&&!role.Description.EndsWith(".desc"),"Role translation missing "+lang);}
  }
  Console.WriteLine("PASS "+checks+" real UI resolver checks in ES EN FR DE IT PT RU ZH JA KO (including runtime language changes)");
 }
}
