using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput;

    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float activeMoveSpeed;
    public float jumpForce = 7f;
    public float gravityMod = 2.5f;

    private float yVelo;

    private Vector3 moveDir;
    private Vector3 movement;
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpack;
    //public float timeBetweenShots = 0.1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f;
    //public float heatPerShot = 1f;
    public float coolRate = 4f;
    public float overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;


    private CharacterController cc;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;
        SwitchGun();

        Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        transform.position = newTrans.position;
        transform.rotation = newTrans.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        CamManage();
        Jump();
        CursorManage();

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
            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutumatic)
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
            if(heatCounter <= 0)
            {
                overHeated = false;

                UIController.instance.overheatedMessage.gameObject.SetActive(false);

            }
        }

        if(heatCounter < 0)
        {
            heatCounter = 0f;
        }
        UIController.instance.weaponTempSlider.value = heatCounter;

        //if(Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        //{
        //    selectedGun++;
        //    if(selectedGun >= allGuns.Length)
        //    {
        //        selectedGun = 0;
        //    }
        //    SwitchGun();
        //}
        //else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        //{
        //    selectedGun--;

        //    if (selectedGun < 0)
        //    {
        //        selectedGun = allGuns.Length - 1;
        //    }
        //    SwitchGun();
        //}

        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                SwitchGun();
            }
        }
    }
    private void CamManage()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotStore += mouseInput.y;
        verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);

        viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);

    }
    private void LateUpdate()
    {
        cam.transform.position = viewPoint.position;
        cam.transform.rotation = viewPoint.rotation;
    }
    private void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveDir = new Vector3(h, 0f, v);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        cc.Move(movement * Time.deltaTime);
    }
    private void Jump()
    {
        // 중력 설정
        yVelo = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
        movement.y = yVelo;

        if (cc.isGrounded)
        {
            movement.y = 0f;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayers);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }
    }
    private void CursorManage()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if(Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;

            }
        }
    }
    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("We hit" + hit.collider.gameObject.name);

            GameObject bulletImpactObject = Instantiate(bulletImpack, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal,  Vector3.up));

            Destroy(bulletImpactObject, 10f);
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
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
}
