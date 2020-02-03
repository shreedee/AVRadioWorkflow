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
     } );    

?>