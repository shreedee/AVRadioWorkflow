﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WordPressPCL;
using WordPressPCL.Models;

namespace components.folderCreator
{
    public partial class FolderCreatorController
    {
        [HttpPost("publish")]
        public async Task Publish([FromBody]FolderDetailsModel data)
        {
            try
            {
                var statusFileName = await SaveFolderAsync(data);

                if (string.IsNullOrWhiteSpace(data.publishDetails.category))
                    throw new bootCommon.ExceptionWithCode("category is required");


                if (string.IsNullOrWhiteSpace(data.publishDetails.title))
                    throw new bootCommon.ExceptionWithCode("title is empty");

                if (string.IsNullOrWhiteSpace(data.publishDetails.bodyText))
                    throw new bootCommon.ExceptionWithCode("bodyText is empty");
                               
                var client = await login.AuthController.InitWP(_wp_url, Request);

                {
                    var imageCount = data.publishDetails.mediaFiles.Where(f => f is mediaList.ImageFileModel && ((mediaList.ImageFileModel)f).canPublish).Count();
                    if(imageCount ==0 )
                        throw new bootCommon.ExceptionWithCode("at least one image is required");

                    imageCount = data.publishDetails.mediaFiles.Where(f => f is mediaList.AuViFileModel ).Count();
                    if (imageCount == 0)
                        throw new bootCommon.ExceptionWithCode("at least one audio/video is required");
                }

                var allCategories =  (await client.Categories.GetAll()).ToArray();

                var category = allCategories.FirstOrDefault(c => c.Name == data.publishDetails.category);
                if (null == category)
                    throw new bootCommon.ExceptionWithCode($"Category {data.publishDetails.category} not found");

                var thePost = await client.Posts.Create(new Post
                {
                    Status = Status.Draft,
                    Title = new Title(data.publishDetails.title),
                    Content = new Content(data.publishDetails.bodyText),
                    Categories = new[] {category.Id }
                });

                var uploadedMedia =await Task.WhenAll( data.publishDetails.mediaFiles.Where(f =>
                {
                    var imagfile = f as mediaList.ImageFileModel;

                    if (null != imagfile && !imagfile.canPublish)
                        return false;

                    if (f is mediaList.OtherFileModel)
                        return false;

                    return true;
                })
                .Select(async (image,i) =>
                {
                    var ext = System.IO.Path.GetExtension(image.fileName).Trim('.');

                    var imageStream = (image is mediaList.ImageFileModel) ?
                        await _storage.getImageStream($"{data.savedFolder}/{image.path}", 800)
                        : await _storage.getStreamAsync($"{data.savedFolder}/{image.path}");

                    var uploaded = await client.Media.Create(imageStream,$"{data.publishDetails.title}_{i}.{ext}");

                    uploaded.Post = thePost.Id;

                    uploaded = await client.Media.Update(uploaded);

                    return new { image, uploaded };
                }));


                var image_gallery = uploadedMedia.Where(f => f.image is mediaList.ImageFileModel)

                    .Select(f => {

                        var image = f.image as mediaList.ImageFileModel;

                        return new
                        {
                            add_a_picture = f.uploaded.Id,
                            photographer = image.photographer,
                            description = image.title
                        };
                    })
                    .ToArray();

                var add_mp3s = uploadedMedia.Where(f => f.image is mediaList.AuViFileModel)
                    .Select(f =>
                    {
                        var image = f.image as mediaList.AuViFileModel;
                        return new
                        {
                            mp3 = f.uploaded.Id,
                            mp3_artists = data.recordingBy,
                            duration = null == image.info?"":image.info.duration.ToString(@"mm\:ss"),
                        };
                    })
                    .ToArray();

                /*
                var post = new {
                    post_status = "draft",
                    post_title = data.publishDetails.title,
                    post_content = data.publishDetails.bodyText,
                };
                */

                var acfFields = new 
                {
                    /*  program_rate
                    1 Can be better : 1 Can be better
                    2 Fairly Ok : 2 Fairly Ok
                    3 Good: 3 Good
                    4 Very good : 4 Very good
                    5 Excellent: 5 Excellent
                    */
                    field_12 = "5 Excellent",

                    /* Time span
                    2 weeks : 2 weeks
                    6 months : 6 months
                    1 year : 1 year
                    more than 1 year : more than 1 year
                     */
                    field_14 = "more than 1 year",

                    field_13 = "5 Excellent",//quality_rate

                    field_15 = "AurovilleRadioTV" ,//place
                    field_17 = data.publishDetails.twiterTitle ,//twitter_text
                    field_18= "AurovilleRadio" ,//episode

                    field_5 = image_gallery ,//"image_gallery" 
                    field_3 = add_mp3s,//add-mp3s
                };

                /*
                var post = new Post()
                {
                    Title = new Title(data.publishDetails.title),
                    Content = new Content(data.publishDetails.bodyText)
                };

                var createdPost = await client.Posts.Create(post);
                */

                var done = await postToWP(client, new {
                    postId = thePost.Id,
                    acfFields,
                    featuredImageId = image_gallery.First().add_a_picture
                });
            }
            catch(Exception ex)
            {
                throw new bootCommon.ExceptionWithCode("failed to publish",innerException: ex);
            }


        }

        static async Task<string> postToWP<T>(WordPressClient client, T data)
        {
            return await client.CustomRequest.Update<T, string>("avupdater/v1/create", data);
        }

        /*
         (
    [__PHP_Incomplete_Class_Name] => WP_Post
    [ID] => 151712
    [post_author] => 525
    [post_date] => 2020-02-01 11:33:54
    [post_date_gmt] => 2020-02-01 06:03:54
    [post_content] => Please listen to the weekly series of readings by Gangalakshmi (in French) from selected works by the Mother and Sri Aurobindo.<br />Today a reading from Essai sur la Guita by Sri Aurobindo, chapt 7 La foi du guerrier Aryen. Images and Mother?s Flowers significance selected from www.blossomlikeaflower.com
<a href='https://www.aurovilleradio.org/wp-content/uploads/import/2020_02_01_production_gangalakshmi_reading_316_french_ed.mp3' />
    [post_title] => Une série hebdomadaire de lectures par Gangalakshmi (en Français) - 315
    [post_excerpt] => Chaque semaine Gangalakshmi sélectionne un extrait des oeuvres de Sri Aurobindo et de Mère en accord avec les vibrations qui correspondent à l'histoire d'Auroville. Weekly selections of Sri Aurobindo and The Mother by Gangalakshmi in French.
    [post_status] => publish
    [comment_status] => open
    [ping_status] => open
    [post_password] => 
    [post_name] => une-srie-hebdomadaire-de-lectures-par-gangalakshmi-en-franais-315-2
    [to_ping] => 
    [pinged] => 
    [post_modified] => 2020-02-01 11:33:57
    [post_modified_gmt] => 2020-02-01 06:03:57
    [post_content_filtered] => 
    [post_parent] => 0
    [guid] => https://www.aurovilleradio.org/une-srie-hebdomadaire-de-lectures-par-gangalakshmi-en-franais-315-2/
    [menu_order] => 0
    [post_type] => post
    [post_mime_type] => 
    [comment_count] => 0
    [filter] => raw
)
        */


        /* ACF get_field
        Array
        (
            [program_rate] => 5 Excellent
            [quality_rate] => 5 Excellent
            [time_span] => more than 1 year
            [place] => AurovilleRadioTV
            [twitter_text] => Chaque semaine Gangalakshmi sélectionne un extrait des oeuvres de Sri Aurobindo et de Mère en accord avec les vibrations qui correspondent à l'histoire d'Auroville. Weekly selections of Sri Aurobindo and The Mother by Gangalakshmi in French.
            [episode] => AurovilleRadio
            [add_video_files] => Array
                (
                )

            [] => 
            [image_gallery] => Array
                (
                    [0] => Array
                        (
                            [add_a_picture] => 151713
                            [photographer] => Nd
                            [description] => Mother's flower
                        )

                )

            [add-mp3s] => Array
                (
                    [0] => Array
                        (
                            [mp3] => 151714
                            [mp3_artists] => Sri Aurobindo
                            [duration] => 47:53
                        )

                )

            [media_gallery] => Array
                (
                )

        )         
         */

    }
}
