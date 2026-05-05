using UnityEngine;
/// <summary>
/// Radar pour l'enemie,il sweep de guache a droite sur une longeure d'arc 
/// retourne la derniere position du player détecter
/// </summary>
public class EnemyPerceptionScan : MonoBehaviour
{
    // les yeux sont un GameObject vide qui représente seulement le transform des yeux (la hauteur et l'endroit par rapport au corps de l'enemi)
    [SerializeField] private Transform eyes;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    //Éventuellement on pourrait changer pour la taille de la map
    [SerializeField] private float visionRange = 15f;

    // si c'est 90 = +- 45 de chaque bord
    [SerializeField] private float radarRangeAngle = 90f;
    [SerializeField] private float radarTurningSpeed = 90f;

    // Pour visualiser si c'est fonctionel
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;

    public bool playerDetected { get; private set; }
    public Vector3 lastKnownPlayerPosition { get; private set; }

    private float currentAngle;
    private float sweepState; // 0 - 1 - 0 - 1 ... 0= left, 1=right


    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;
        visionRange = cfg.enemyVisionRange;
        radarRangeAngle = cfg.enemyRadarRangeAngle;
        radarTurningSpeed = cfg.enemyRadarTurningSpeed;
    }

    private void Awake()
    {
        if (eyes == null) throw new MissingComponentException("Il faut assigner des yeux");
        
        // Au lieu de faire des asserts inutiles, on ajoute les valeurs si elles ne sont pas bonnes, moins lourds durant l'entrainement
        if (radarRangeAngle <= 0f) radarRangeAngle = 90f;
        if (radarTurningSpeed <= 0f) radarTurningSpeed = 90f;
        if (visionRange <= 0f) visionRange = 15f;
    }

    /// <summary>
    /// l'angle avance linéairement de -halfArc a halfArc 
    /// Commence a gauche
    /// Modifie le CurrentAngle indépendemment.
    /// </summary>
    private void MoveRadarAngle()
    {
        float halfArc = radarRangeAngle * 0.5f;

        // meme si le radarTurningSpeed est invalide, le raycast va etre fait, mais va rester a gauche par défaut
        if (radarTurningSpeed <= 0)
        {
            currentAngle = -halfArc; 
            return;
        }

        // vitesse en term de l'arc
        float stateSpeed = radarTurningSpeed / radarRangeAngle;
                            
        sweepState += stateSpeed * Time.deltaTime;

        //  c'est un Triangle Sin wave, ou la valeur est incrémenter de 0 a 1 et retombe a 0  -> /\/\/ intervale [0,1]
        // arrivé a 1, redescend a 0, ...
        float pingPong = Mathf.PingPong(sweepState, 1f);

        // Converti le pingPong qui est de [0,1] a l'angle
        currentAngle = Mathf.Lerp(-halfArc, +halfArc, pingPong);
    }
    
    /// <summary>
    /// Fonction qui est appellé dans le Update du EnemyBrainLogic
    /// retourne true si le player a été retrouvé cet frame-la
    /// early return si le player est en dehors du range, lame a double tranchant
    /// </summary>
    /// <param name="player"> la transform du player </param>
    /// <returns></returns>
    public bool UpdatePerception(Transform player)
    {
        playerDetected = false;

        // on commence le scan radar
        MoveRadarAngle();

        if (player == null)
        {
            Debug.LogWarning(" la transform du player n'est pas connecté");
            return false;
        }

        Vector3 origine = eyes.position;

        // early return si le player est plus loin que le range
        // lame a double tranchant, car peut etre durant l'intervalle du scan le player bouge dans le range
        // A voir...
        if ((player.position - origine).sqrMagnitude > visionRange * visionRange)
            return false;

        // yaw, la direction du raycast
        Vector3 direction = Quaternion.AngleAxis(currentAngle, Vector3.up)* eyes.forward;
        direction.y = 0;
        direction.Normalize();

        // le out veut dire que le raycast retourne une variable hit si c'est true. Cette variable hit comporte les informations de ce qu'elle a hit ( distance, collider,position ...)
        if( Physics.Raycast( origine, direction,out RaycastHit hit, visionRange, obstacleLayer | playerLayer ))
        {
            // l'indexe du player layer dans l'inspector
            // ex: l'enemi est dans le player est layer 6
            int hitLayer = hit.collider.gameObject.layer;

            // Convertit en binaire, car c'est comme ca que Unity peut l'interpréter
            int hitLayerMask = 1 << hitLayer;

            // regarde si le hitLayerMask existe dans les layers du player, et !=0 convertit et inverse le bool zéro = false, pas zéro = true
            // Donc si le hitLayerMask et le player:  bool isPlayer = (6 & 6 (le layer du player) ) != 0; est équivalent
            bool isPlayer = (hitLayerMask & playerLayer) != 0;

            if (isPlayer)
            {
                playerDetected = true;
                lastKnownPlayerPosition = hit.transform.position;
                return true;
            }
        }
        return false;
    }



    //                                                    GIZMOS


    // a verifier a la fin
    #region Gizmo Region
    private void OnDrawGizmos()
    {
        if (!drawGizmos || drawOnlyWhenSelected)
            return;

        DrawGizmosInternal();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !drawOnlyWhenSelected)
            return;

        DrawGizmosInternal();
    }

    private void DrawGizmosInternal()
    {
        if (eyes == null)
            return;

        // Vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyes.position, visionRange);

        Vector3 baseForward = eyes.forward;

        // Current sweep ray
        Vector3 rayDir = Quaternion.AngleAxis(currentAngle, Vector3.up) * baseForward;
        rayDir.y = 0f;

        if (rayDir.sqrMagnitude > 0.0001f)
        {
            rayDir.Normalize();
            Gizmos.color = playerDetected ? Color.green : Color.red;
            Gizmos.DrawRay(eyes.position, rayDir * visionRange);
        }

        // Arc boundaries
        float halfArc = radarRangeAngle * 0.5f;

        Vector3 left = Quaternion.AngleAxis(-halfArc, Vector3.up) * baseForward;
        Vector3 right = Quaternion.AngleAxis(+halfArc, Vector3.up) * baseForward;
        left.y = 0f; right.y = 0f;

        if (left.sqrMagnitude > 0.0001f) left.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        Gizmos.color = Color.gray;
        Gizmos.DrawRay(eyes.position, left * visionRange);
        Gizmos.DrawRay(eyes.position, right * visionRange);

        if (playerDetected)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastKnownPlayerPosition, 0.2f);
        }
    }
}

#endregion