Ce plugin permet de faciliter la mise en place d'un lobby

Pour que ce plugin marche, il faut installer les packages de unity :
- netcode for gameobjects
- lobby
- relay
- multiplayer tool (pas obligatoire mais conseille)

Autre package utile pour les tests : 
- ParrelSync : https://github.com/VeriorPies/ParrelSync

Tutos que j'ai suivi qui m'ont aidé à faire ce plugin : 
- https://www.youtube.com/watch?v=3yuBOB3VrCk (netcode for gameobjects)
- https://www.youtube.com/watch?v=-KDlEBfCBiU&t=1067s (unity lobby)
- https://www.youtube.com/watch?v=msPNJ2cxWfw (unity relay)

Unity Gaming Services : https://dashboard.unity3d.com/

Ce plugin utilise les Assembly definitions

Dans Multiplayer -> Lobby_ -> LobbyManager.cs : les methodes generales pour gerer le lobby
                           -> KeysTypeEnum : necessaire pour LobbyManager
                           -> PlayerInfos et RoomInfos : necessaire pour LobbyManager
                           -> ChangeScene et PlayerCheck : scripts pour changer de s cene lors du lancement du jeu (PlayerCheck doit etre mis sur chaque joueur et va envoyer un event attraper par ChangeScene qui va load la scene pour tous les joueurs)
Dans Multiplayer -> PlayerTeamEnum.cs : Necessaire pour l'exemple
Dans UI -> Tous les scripts servent d'exemple pour un lobby
Dans scene -> des scenes d'exemple
Dans Prefabs -> prefabs d'exemple
                  
