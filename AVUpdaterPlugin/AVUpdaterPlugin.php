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

   //invoked as https://www.aurovilleradio.org/wp-json/avupdater/v1/fromfile?fileName=test.json
    function av_updater_fromfile_func( WP_REST_Request $request ) {

        try {

            $fileName = $request['fileName'];

            $str = file_get_contents('/pmon/'.$fileName);
            if(!$str)
                throw new Exception('failed to get content');

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

                throw new Exception("escape");

                $postData["post_status"]='draft';
                $postID = wp_insert_post($postData, true);
    
            }


            if( is_wp_error($postID) ){
                throw new Exception("failed to upsert post :"
                . $postID->get_error_message()
                ." JSON DATA ". json_encode( serialize($postData))
                ." jsonOrginal ". json_encode( serialize($jsonOrginal))
                );
            }

            $updatedAttachments=array();
            $wordpress_upload_dir = wp_upload_dir();
            foreach($jsonOrginal['attachments'] as $attachment) {
                
                $attachment['path'] = $wordpress_upload_dir['path'] . '/' . $attachment['fileName'];

                $attachment['guid'] = $wordpress_upload_dir['url'] . '/' . $attachment['fileName'];
                
                if(!empty($attachment['isImage'])){
                    require_once( ABSPATH . 'wp-admin/includes/image.php' );
                    require_once(ABSPATH . 'wp-admin/includes/file.php');

                    $file_type = wp_check_filetype(basename($attachment['fileName']), null);

                    $attachment['post_mime_type'] =$file_type['type'];
                }
                
                /*
                $newFile=[
                    =>
                    //'mime'=>(new finfo(FILEINFO_MIME))->file(  $attachment['fileName'])
                ];
                */
                
                $newFileID = wp_insert_attachment($attachment,$attachment['path'],$postID,true);

                if( is_wp_error($newFileID) ){
                    $attachment['error']=$newFileID->get_error_message();
                }else{
                    $attachment['ID'] = $newFileID;

                    if(!empty($attachment['isImage'])){


                        $attach_data = wp_generate_attachment_metadata( $newFileID, $attachment['path'] );
                        wp_update_attachment_metadata( $newFileID, $attach_data );
                        
                        $attachment['attach_data']=$attach_data;

                    }

                }

                array_push($updatedAttachments,$attachment);
                
            }

            foreach($jsonOrginal['acfFields'] as $tfield=>$tVal) { 
                update_field($tfield,$tVal,$postID);
            }

            if(!empty($jsonOrginal['featuredImageId'])){
                set_post_thumbnail($postID,$jsonOrginal['featuredImageId']);
            }

            $acf_fields = get_fields( $postID );
            $post_fields = get_post($postID);
            
            $done = (object) [
                'success'=>true,
                'action'=>$action,
                'postID'=>$postID,
                'post_fields'=>$post_fields,
                'acf_fields'=>$acf_fields,
                'updatedAttachments'=>$updatedAttachments
            ];
            
            //$done='hhhh';

            //return json_encode(serialize( $done));
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