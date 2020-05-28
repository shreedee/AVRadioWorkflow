<?php
   /*
   Plugin Name: AVRadio Custom site updater
   Plugin URI: http://my-awesomeness-emporium.com
   description: >-
  a plugin to create and update AV posts using REST API
   Version: 1.0
   Author: Shree DEE
   License: GPL2
   */

    function removeDirectory($path) {

        $files = glob($path . '/*');
        foreach ($files as $file) {
            is_dir($file) ? removeDirectory($file) : unlink($file);
        }
        rmdir($path);

        return;
    }

   //invoked as https://www.aurovilleradio.org/wp-json/avupdater/v1/fromfile?fileName=test.json
    function av_updater_fromfile_func( WP_REST_Request $request ) {

        try {

            $baseFolderName ='/pmon/'.$request['baseFolderName'];
            $fileName = $baseFolderName .'/publishData.json';

            

            if(!file_exists($fileName))
                throw new Exception('file not found '. $request['baseFolderName']);

            $str = file_get_contents($fileName);
            if(!$str)
                throw new Exception('failed to get content for fileName :'.$fileName);

            $jsonOrginal = json_decode($str, true);
            if (JSON_ERROR_NONE !== json_last_error()) {
                throw new RuntimeException('Unable to parse response body into JSON: ' . json_last_error());
            }
        
            $postData=$jsonOrginal["post"];

            $postData["post_excerpt"]=wp_trim_excerpt( $postData["post_content"]);

            $postID =0;
            $action = "newPost";
            if(!empty($postData["ID"])){

                $action = "updatedPost";
                $postID = wp_update_post($postData, true);

            }else{

                if(empty($postData["post_status"])){
                    $postData["post_status"]='draft';
                }
                
                $postID = wp_insert_post($postData, true);
    
            }


            if( is_wp_error($postID) ){
                throw new Exception("failed to upsert post :"
                . $postID->get_error_message()
                ." JSON DATA ". json_encode( serialize($postData))
                ." jsonOrginal ". json_encode( serialize($jsonOrginal))
                );
            }

            if("newPost" == $action){
                /*  program_rate
                    1 Can be better : 1 Can be better
                    2 Fairly Ok : 2 Fairly Ok
                    3 Good: 3 Good
                    4 Very good : 4 Very good
                    5 Excellent: 5 Excellent
                    */
                update_field("field_12","5 Excellent",$postID);


                /* Time span
                    2 weeks : 2 weeks
                    6 months : 6 months
                    1 year : 1 year
                    more than 1 year : more than 1 year
                     */
                update_field("field_14","more than 1 year",$postID);

                //quality_rate
                update_field("field_13","5 Excellent",$postID);

                //place
                update_field("field_15","AurovilleRadioTV",$postID);

                //episode
                update_field("field_18","AurovilleRadio",$postID);
            }

            if(!empty($jsonOrginal['twiterTitle'])){
                update_field("field_17",$jsonOrginal['twiterTitle'],$postID);
            }


            $updatedAttachments=array();
            $wordpress_upload_dir = wp_upload_dir();
            $featuredImagId = null;
            foreach($jsonOrginal['attachments'] as $attachment) {

                $wpPath = $attachment['wpPath'] = $wordpress_upload_dir['path'] . '/' . $attachment['fileName'];

                $attachment['guid'] = $wordpress_upload_dir['url'] . '/' . $attachment['fileName'];


                $file_type = wp_check_filetype(basename($attachment['fileName']), null);
                
                if(!empty($attachment['isImage'])){
                    require_once( ABSPATH . 'wp-admin/includes/image.php' );
                    require_once(ABSPATH . 'wp-admin/includes/file.php');

                    $attachment['post_mime_type'] =$file_type['type'];
                }

                $src= $baseFolderName . '/' . $attachment['path'];
                if(!copy($src,$wpPath)){
                    throw new Exception("Failed to copy :".$src." -> ".$wpPath);
                }
                
                                
                $newFileID = wp_insert_attachment($attachment,$wpPath,$postID,true);

                if( is_wp_error($newFileID) ){
                    $attachment['error']=$newFileID->get_error_message();
                }else{
                    $attachment['ID'] = $newFileID;

                    if(!empty($attachment['isImage'])){

                        $attach_data = wp_generate_attachment_metadata( $newFileID, $attachment['wpPath'] );
                        wp_update_attachment_metadata( $newFileID, $attach_data );
                        
                        $attachment['attach_data']=$attach_data;

                        if(empty($featuredImagId)){
                            set_post_thumbnail($postID,$newFileID);
                            $featuredImagId =$newFileID;
                        }

                        $scfFiled = get_field("field_5",$postID);
                        if(empty($scfFiled)){
                            $scfFiled =[];
                        }
                        array_push($scfFiled,[
                            "add_a_picture"=>$newFileID
                        ]);

                        update_field("field_5",$scfFiled,$postID);


                    }else{

                        $scfFiled = get_field("field_3",$postID);
                        if(empty($scfFiled)){
                            $scfFiled =[];
                        }

                        $ext = $file_type["ext"];

                        array_push($scfFiled,[
                            $ext=>$newFileID
                        ]);

                        update_field("field_3",$scfFiled,$postID);


                    }

                }

                array_push($updatedAttachments,$attachment);
                
            }

            

            $acf_fields = get_fields( $postID );
            $post_fields = get_post($postID);
            
            $done = (object) [
                'success'=>true,
                'action'=>$action,
                'postID'=>$postID,
                'post_fields'=>$post_fields,
                'acf_fields'=>$acf_fields,
                'featuredImagId'=>$featuredImagId,
                'updatedAttachments'=>$updatedAttachments,
                'baseFolderName'=>$baseFolderName
            ];
            
            removeDirectory($baseFolderName);

            return new WP_REST_Response($done);

        } catch (Exception $e) {
            return  new WP_REST_Response((object) [
                'failed'=>true,
                'error'=>serialize($e),
            ]);
        }
    }

    function av_updater_create_func( WP_REST_Request $request ) {

        try {
            if(!current_user_can('edit_others_posts'))
                throw new Exception('Access denied');

            $parameters = $request->get_json_params();

            //$newId = wp_insert_post( $parameters['post']);
            $newId = $parameters['postId'];

            set_post_thumbnail($newId,$parameters['featuredImageId']);

            //$post_fields = get_fields( 151712 );
            //$post_fields = get_post(151712);

            foreach($parameters['acfFields'] as $tfield=>$tVal) { 
                update_field($tfield,$tVal,$newId);
            }

            return json_encode(serialize('success: '. $newId));
        } catch (Exception $e) {
            return  json_encode(serialize('error: '. $e->getMessage()));
        }
    }

    add_action( 'rest_api_init', function () {
        register_rest_route( 'avupdater/v1', '/create', array(
          'methods' => 'POST',
          'callback' => 'av_updater_create_func',
        ) );

        
        register_rest_route( 'avupdater/v1', '/fromfile', array(
            'methods' => 'GET',
            'callback' => 'av_updater_fromfile_func',
          ) );
     } );    

?>