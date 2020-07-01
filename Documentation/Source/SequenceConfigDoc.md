
# Sequence Configuration File Documentation :

**[English version :](#_3pikm9tmz4s2) 2**

[Preamble :](#_ugzsm57pntkg) 2

[Definition :](#_81amkmwjmlfv) 2

[Usage :](#_sn52fecapg6o) 2

[Sequence List Element :](#_ddmg9qfigejt) 2

[Sequence Element :](#_wup912j41tkc) 3

[Marker Element :](#_oqocyqg350d) 3

[Tips :](#_6qcxp0j6mvr1) 3

**[French version :](#_oc2v50u5cfnb) 4**

[Préambule :](#_n1skw5tiz0av) 4

[Définition :](#_3jlqpq851t7f) 4

[Utilisation :](#_tnsfdv4ej8rg) 4

[L&#39;élément &quot;Sequence List&quot; :](#_rfe68qkbsrs4) 5

[L&#39;élément &quot;Sequence&quot; :](#_rmz6h1emujf4) 5

[L&#39;élément &quot;Marker&quot; :](#_5onafnqwtch8) 5

[Astuce :](#_k4qb5k1d3wfy) 6

##


##


## English version :

## Preamble :

First, if you have done some tweak to the original file and you want to check if you didn&#39;t do any mistakes, without reading all the following explanations, know that you can.

Just head to : [https://www.freeformatter.com/xml-validator-xsd.html](https://www.freeformatter.com/xml-validator-xsd.html)

And either copy-paste your file on the XML section or upload it, then do the same thing for the XSD file called &quot;SequencesDefinition.xsd&quot;, that is located under the root folder of the project.

Basically, this XSD file is a set of rules for this specific custom XML format. And if there is any mistake on the XML file, the website will clearly warn you about what it is and where it is.

Also, if you are really new to XML format, you can open the XML file &quot;ConfigSequenceExample.xml&quot;, that you will find next to this documentation file. It is a concrete example of what you can do. You can open it on your browser, it will display nicely, but it won&#39;t show the notes. For this, use any text-editing program.

## Definition :

This XML file is a configuration file that is meant to be used with the LSL Kinect program. It defines a sequence of action for this program. Since it&#39;s written on XML, it follows all the XML classic conventions and it&#39;s extensible.

If you want to add infos to this file, you can, but be sure to update the XSD file that go along with, and this documentation.

## Usage :

For this file to be detected by the program, it needs to be located on the same folder than the .exe file and be named &quot;SequenceConfig.xml&quot;.

This file is really basic, it&#39;s only compound with three elements. These three elements will be automatically transform into object by the program, and then use by it. So, any mistakes will cause the program to fail to load the instructions contained in these objects.

All the elements tag are case-sensitives.

The elements order at the same level doesn&#39;t matter.

### Sequence List Element :

This is the root element, the one that need to contains all the others, it has to be called &quot;sequenceList&quot;.

- This element needs to contain at least one &quot;sequence&quot; element.

### Sequence Element :

The sequence element, has to be called &quot;sequence&quot;, is purpose is to define a list of actions for the program to interpret.

- It has an element &quot;name&quot;, that will be displayed on the program, for the user to recognize and select it.
- And it has a list of &quot;marker&quot; elements, that cannot be empty.

### Marker Element :

The marker element, has to be called &quot;marker&quot;, this is the one that truly define the action the program will execute. Every action defined here will cause a marker, that described the action, to be sent on the LSL broadcast.

- It has an attribute called &quot;affectCSV&quot;, that can only be &quot;true&quot; or &quot;false&quot;. It set if the action should be executed on the CSV too. (Only for the start and stop action). By default it&#39;s set to &quot;false&quot;. Notice that, even if the actions don&#39;t affect the CSV flow, they will be note down on the CSV marker file anyway, as marker.
- It has a child element called &quot;content&quot;. This is the action&#39;s description. This description will be sent by the program into a LSL marker when the corresponding action will start. It is also displayed on the program UI when you select the sequence it belongs to.
- It has a child element called &quot;type&quot;. This is the most important one, it determine which action the program has to do when it find this marker. You can choose between three action type :
  - Start : Start the LSL broadcast (and the CSV recording if requested).
  - Stop : Stop the LSL broadcast (and the CSV recording if requested).
  - Message : Send a marker containing the &quot;content&quot; element on the LSL stream. It is only useful for the user that will manipulate the XDF output later on.

Even if, there are two action-type called &quot;Start&quot; and &quot;Stop&quot;, a sequence can contains as many &quot;start&quot; and &quot;stop&quot; as you need.

## Tips :

If you want to add notes within the XML file, you can add a comment section using these balise :

\<!-- This is a comment -->

## French version :

## Préambule :

Tout d&#39;abord, si vous avez fait des changements au fichier d&#39;origine et que vous voulez vérifier que vous n&#39;avez pas fait d&#39;erreur, sans lire les explications ci-après. Sachez que vous pouvez.

Rendez-vous sur : [https://www.freeformatter.com/xml-validator-xsd.html](https://www.freeformatter.com/xml-validator-xsd.html)

Et soit, copiez-collez le contenu du fichier XML dans la section qui correspond, soit uploadez le fichier. Puis faite la même chose pour le fichier XSD appelé &quot;SequencesDefinition.xsd&quot;, qui se trouve à l&#39;adresse dans le dossier racine du projet.

Pour faire simple, ce fichier XSD contient les règles spécifiques au format de ce fichier XML. Et si le fichier XML contient des erreurs, le site web va vous avertir et vous désigner leur emplacement.

De plus, si vous êtes vraiment nouveau au format XML, vous pouvez ouvrir le fichier XML &quot;ConfigSequenceExample.xml&quot;, que vous trouverez à côté de ce fichier de documentation. C&#39;est un exemple concret de ce que vous pouvez faire.

Vous pouvez l&#39;ouvrir sur votre navigateur, il s&#39;affichera bien, mais il ne montrera pas les notes. Pour cela, utilisez n&#39;importe quel programme d&#39;édition de texte.

## Définition :

Ce fichier XML est un fichier de configuration destiné à être utilisé avec le programme LSL Kinect. Il définit une séquence d&#39;actions pour ce programme. Puisqu&#39;il est écrit en XML, il suit toutes les conventions classiques de XML et il est extensible.

Si vous souhaitez ajouter des informations à ce fichier, vous pouvez, mais assurez-vous de mettre à jour le fichier XSD qui va avec, ainsi que cette documentation.

## Utilisation :

Pour que ce fichier soit détecté par le programme, il doit se trouver dans le même dossier que le fichier .exe et être nommé «SequenceConfig.xml».

Ce fichier est vraiment basique, il n&#39;est composé que de trois éléments. Ces trois éléments seront automatiquement transformés en objet par le programme, puis utilisés par celui-ci. Ainsi, toute erreur entraînera l&#39;échec du programme, il ne pourra donc pas charger les instructions contenues dans ces objets.

Tous les éléments sont sensibles à la casse.

L&#39;ordre des éléments au même niveau n&#39;a pas d&#39;importance.

### L&#39;élément &quot;Sequence List&quot; :

Il s&#39;agit de l&#39;élément racine, celui qui doit contenir tous les autres, il doit être appelé «sequenceList».

- Cet élément doit contenir au moins un élément &quot;sequence&quot;.

### L&#39;élément &quot;Sequence&quot; :

L&#39;élément séquence, doit être appelé «sequence» et a pour but de définir une liste d&#39;actions que le programme doit interpréter.

- Il possède un élément &quot;name&quot;, qui sera affiché sur le programme, pour que l&#39;utilisateur le reconnaisse et le sélectionne.
- Et il a une liste d&#39;éléments &quot;marker&quot;, d&#39;au moins un élément

### L&#39;élément &quot;Marker&quot; :

L&#39;élément marqueur, doit être appelé &quot;marker&quot;, c&#39;est celui qui définit vraiment l&#39;action que le programme va exécuter. Chaque action définie ici entraînera l&#39;envoi d&#39;un marqueur, qui décrit l&#39;action, sur la diffusion LSL.

- Il possède un attribut appelé «affectCSV», qui ne peut être que «true» ou «false». Il définit si l&#39;action doit également être exécutée sur le CSV. (Uniquement pour l&#39;action de démarrage et d&#39;arrêt). Par défaut, il est défini sur &quot;false&quot;. Notez que, même si les actions n&#39;affectent pas le flux CSV, elles seront de toute façon notées dans le fichier de marqueur CSV, en tant que marqueur.
- Il a un élément enfant appelé «contenu». Ceci est la description de l&#39;action. Cette description sera envoyée par le programme dans un marqueur LSL lorsque l&#39;action correspondante commencera. Il est également affiché sur l&#39;interface utilisateur du programme lorsque vous sélectionnez la séquence à laquelle il appartient.
- Il a un élément enfant appelé &quot;type&quot;. C&#39;est le plus important, il détermine quelle action le programme doit faire quand il trouve ce marqueur. Vous pouvez choisir entre trois types d&#39;actions:
  - Start : Démarre la diffusion LSL (et l&#39;enregistrement du CSV si demandé).
  - Stop : Arrête la diffusion LSL (et l&#39;enregistrement du CSV si demandé).
  - Message : Envoie un marqueur contenant l&#39;élément «content» sur le flux LSL. Il n&#39;est utile que pour l&#39;utilisateur qui manipulera le fichier de sortie XDF ultérieurement.

Même s&#39;il existe deux types d&#39;action appelés «Start» et «Stop» , une séquence peut contenir autant de «démarrage» et d&#39;«arrêt» que vous le souhaitez.

## Astuce :

Si vous souhaitez ajouter des notes à l&#39;intérieur du fichier XML, vous pouvez ajouter une section de commentaire en utilisants ces balises :

\<!-- Ceci est un commentaire -->