using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("移動與速度設定")]
    public float forwardSpeed = 15f;
    public float sideSpeed = 12f;

    [Header("音效設定")]
    public AudioSource engineAudioSource; 
    public AudioClip crashSound;          
    public AudioClip passSound;           

    private Rigidbody rb;
    private bool isControllable = true;
    
    // 新增：用來記錄現在是不是正在過關靜音期間
    private bool isPassingMuted = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = false; 
        }

        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            engineAudioSource.playOnAwake = true;
            if (!engineAudioSource.isPlaying) engineAudioSource.Play();
        }
    }

    void Update()
    {
        if (!isControllable) return;

        float hInput = Input.GetAxis("Horizontal"); 
        float vInput = Input.GetAxis("Vertical");   

        // 移動邏輯
        float forwardMovement = vInput * forwardSpeed * Time.deltaTime;
        transform.Translate(Vector3.forward * forwardMovement, Space.World);

        float sideMovement = hInput * sideSpeed * Time.deltaTime;
        transform.Translate(Vector3.right * sideMovement, Space.World);

        // 【核心修正點】：只有在「沒有被過關靜音」的狀態下，Update 才可以控制飛行音量
        if (!isPassingMuted && engineAudioSource != null && engineAudioSource.isPlaying)
        {
            if (Mathf.Abs(hInput) > 0.1f || Mathf.Abs(vInput) > 0.1f)
            {
                engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, 1.0f, Time.deltaTime * 5f);
            }
            else
            {
                engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, 0.3f, Time.deltaTime * 5f);
            }
        }

        float tiltAngle = -hInput * 25f; 
        transform.rotation = Quaternion.Euler(0, 0, tiltAngle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 加上 !isPassingMuted 防止飛機在方塊內連續觸發多次
        if (other.CompareTag("Goal") && isControllable && !isPassingMuted)
        {
            Debug.Log("【作業回報】得分！順利通過障礙物！");
            StartCoroutine(PlayPassSoundOnly());
        }
    }

 IEnumerator PlayPassSoundOnly()
    {
        isPassingMuted = true; // 1. 啟用靜音鎖

        if (engineAudioSource != null)
        {
            engineAudioSource.volume = 0f; 
            engineAudioSource.Pause();     // 2. 暫停飛行引擎聲
        }

        if (passSound != null)
        {
            // 【音量強化寫法】：直接借用 Camera 上的 AudioSource 或臨時創建來播放
            // 這裡使用 PlayOneShot 並指定音量倍數為 1.5f (最高可調至 2.0f 或 3.0f 來爆音放大)
            if (Camera.main != null)
            {
                AudioSource camAudio = Camera.main.GetComponent<AudioSource>();
                if (camAudio == null) camAudio = Camera.main.gameObject.AddComponent<AudioSource>();
                
                camAudio.spatialBlend = 0f; // 強制設定為 2D 音效（全音量，不受 3D 距離衰減）
                
                // 【核心修改】：最後的參數 2.0f 代表放大到原本 100% 音量的 2 倍大聲！
                camAudio.PlayOneShot(passSound, 2.0f); 
            }
            
            yield return new WaitForSeconds(passSound.length); 
        }
        else
        {
            yield return new WaitForSeconds(1.0f); 
        }

        // 5. 成功音效播完了，恢復飛行聲
        if (isControllable && engineAudioSource != null)
        {
            engineAudioSource.UnPause();
            engineAudioSource.volume = 0.5f; 
        }

        isPassingMuted = false; // 6. 解開靜音鎖
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isControllable)
        {
            Debug.Log("【作業回報】糟糕，撞到障礙物了！");
            isControllable = false; 
            isPassingMuted = true; // 鎖死音效

            StopAllCoroutines();

            if (engineAudioSource != null)
            {
                engineAudioSource.Stop(); // 徹底切斷飛行聲
            }

            if (crashSound != null)
            {
                AudioSource.PlayClipAtPoint(crashSound, Camera.main.transform.position, 1.0f);
            }
        }
    }
}