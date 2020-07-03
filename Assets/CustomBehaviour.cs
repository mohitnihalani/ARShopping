/**************************************************************************
* Copyright (C) echoAR, Inc. 2018-2020.                                   *
* echoAR, Inc. proprietary and confidential.                              *
*                                                                         *
* Use subject to the terms of the Terms of Service available at           *
* https://www.echoar.xyz/terms, or another agreement                      *
* between echoAR, Inc. and you, your company or other organization.       *
***************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomBehaviour : MonoBehaviour
{
    [HideInInspector]
    public Entry entry;

    // Boolean field to check if the current gameObject is selected in dropdown
    public bool active;

    public bool completelyLoaded = false;

    /// <summary>
    /// EXAMPLE BEHAVIOUR
    /// Queries the database and names the object based on the result.
    /// </summary>

    // Use this for initialization

    public string identifier;

    // URL of the product
    public string productURL;

    //Price of the product
    public string priceOfProduct;

    // Use this for initialization
    void Start()
    {
        // Add RemoteTransformations script to object and set its entry
        this.gameObject.AddComponent<RemoteTransformations>().entry = entry;
       
        active = false;

        identifier = this.gameObject.name;
        // Qurey additional data to get the name
        string value = "";
        if (entry.getAdditionalData() != null && entry.getAdditionalData().TryGetValue("price", out value))
        {
            // Set Price
            priceOfProduct = value;
        }

        if (entry.getAdditionalData() != null && entry.getAdditionalData().TryGetValue("URL", out value))
        {
            // URL
            productURL = value;
        }else {
            productURL = null;
        }
    }

    // Update is called once per frame
    void Update()
    {      

        // Check if all the assets are completely loaded.
        if(!completelyLoaded){
           var glbAsset = GetComponentInChildren<GLTFast.GlbAsset>();
           if(glbAsset == null){
               completelyLoaded = true;
           }
        }
        if(!active){
            foreach (Renderer r in this.gameObject.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }else {
            // If active make the product visible by enabling renderer
            foreach (Renderer r in this.gameObject.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }

    }
}