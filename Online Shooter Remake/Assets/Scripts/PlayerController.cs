using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Transform viewPoint;
    [SerializeField]
    private float mouseSensitivity = 1f;
    [SerializeField]
    private float verticalRotStore;

    private Vector2 mouseInput;

    [SerializeField]
    private bool InvertLook;

    [SerializeField]
    private float moveSpeed = 5f , runSpeed = 8f;

    private float activeMoveSpeed;

    private Vector3 moveDir, movement;

    [SerializeField]
    private CharacterController charCon;

    private Camera cam;

    [SerializeField]
    private float jumpForce = 12f;
    [SerializeField]
    private float gravityMod = 2.5f;


    [SerializeField]
    private Transform groundCheckPoint;

    private bool isGrounded;

    [SerializeField]
    private LayerMask groundLayers;


    [SerializeField]
    private GameObject bulletImpact;

    //[SerializeField]
    //private float timeBetweenShots = .1f;

    private float shotCounter;

    public float muzzleDisplayTime;
    private float muzzleCounter;


    [SerializeField]
    private float maxHeat = 10f,/* heatPerShot = 1f,*/ coolRate = 4f, overHeatCoolRate = 5f;

    private float heatCounter;

    private bool overHeated;

    [SerializeField]
    private Gun[] allGuns;

    private int selectedGun;


    public GameObject playerHitImpact;


    public int maxHealth = 100;
    private int currentHealth;


    public Animator anim;

    public GameObject playerModel;


    public Transform modelGunPoint, gunHolder;


    public Material[] allSkins;

    public float adsSpeed = 5f;

    public Transform adsOutPoint, adsInPoint;

    public AudioSource footStepSlow, footStepfast;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;


        //SwitchGun();

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);
        
        currentHealth = maxHealth;


        //Transform newTrans = SpawnManager.instance.GetSpawnPoint();

        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;


        if (photonView.IsMine)
        {
            playerModel.SetActive(false);


            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];
    }

   
    void Update()
    {
        if (photonView.IsMine)
        {

            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotStore += mouseInput.y;
            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

            if (InvertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;

                if(!footStepfast.isPlaying && moveDir != Vector3.zero)
                {
                    footStepfast.Play();
                    footStepSlow.Stop();
                }
            }
            else
            {
                activeMoveSpeed = moveSpeed;


                if (!footStepSlow.isPlaying && moveDir != Vector3.zero)
                {
                    footStepfast.Stop();
                    footStepSlow.Play();
                }
            }

            if(moveDir == Vector3.zero || !isGrounded)
            {
                footStepfast.Stop();
                footStepSlow.Stop();
            }

            float yVel = movement.y;

            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;

            movement.y = yVel;

            if (charCon.isGrounded)
            {
                movement.y = 0f;
            }


            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayers);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if (muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
            }

            if (!overHeated)
            {

                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;

            }
            else
            {
                heatCounter -= overHeatCoolRate * Time.deltaTime;

                if (heatCounter <= 0)
                {
                    overHeated = false;

                    UIController.instance.overHeatedMessage.gameObject.SetActive(false);

                }
            }

            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }

            UIController.instance.weaponTempSlider.value = heatCounter;



            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }

                // SwitchGun();


                photonView.RPC("SetGun", RpcTarget.All, selectedGun); 
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;

                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                //SwitchGun();


                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchGun();

                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }

            anim.SetBool("grounded", isGrounded);
            anim.SetFloat("speed", moveDir.magnitude);


            if (Input.GetMouseButton(1))
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, allGuns[selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime); 

            }



            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("We hit :" + hit.collider.gameObject.name);

            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit : " + hit.collider.gameObject.GetPhotonView().Owner.NickName);

                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All , photonView.Owner.NickName , allGuns[selectedGun].shotDamage , PhotonNetwork.LocalPlayer.ActorNumber);   

            }

            else
            {
                GameObject bulletImpactobject = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

                Destroy(bulletImpactobject, 7f);
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;


        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;

            UIController.instance.overHeatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
    }

    [PunRPC]
    public void DealDamage(string damager , int damageAmount , int actor )
    {
        TakeDamage(damager , damageAmount , actor);
    }

    public void TakeDamage(string damager, int damageAmount , int actor)
    {

        if (photonView.IsMine)
        {
            //Debug.Log(photonView.Owner.NickName + " Has been Hit..by " + damager);

            currentHealth -= damageAmount;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
            PlayerSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatsSend(actor , 0 , 1);
            }


            UIController.instance.healthSlider.value = currentHealth;

        }
    }


    private void LateUpdate()
    {

        if (photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position = viewPoint.position;
                cam.transform.rotation = viewPoint.rotation;
            }
            else
            {
                cam.transform.position = MatchManager.instance.mapCamPoint.position;
                cam.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
            
        }
    }


    void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }
}
