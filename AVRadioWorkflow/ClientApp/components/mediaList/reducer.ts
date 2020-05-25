import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import { checkFetchError } from '../../bootCommon/asyncLoader';
import ensureWaitBox from '../waitBox/reducer';
import * as sparkMD5 from 'spark-md5';
import * as sha256 from 'js-sha256';


import * as Evaporate from '../../../pullRequest/evaporateJS';

import { DirectUploadModel } from '../../generated/DirectUploadModel';
import { MediaFileBaseModel } from '../../generated/MediaFileBaseModel';

import * as _ from 'lodash';

type MediaObjects = {
    //not uploaded file system files
    files: File[];

    //already uploaded files
    mediaList: { [mediaType: string]: MediaFileBaseModel[] };
}

export interface IMediaFilesState {
    readonly objectList: { [galleyId: string]: MediaObjects};
    readonly hasDragOver: { [galleyId: string]: boolean };

    readonly selectedObjectType: string;
};


export type SavedMediaProps = {
    mediaFiles: MediaFileBaseModel[];
    filesystemLink: string;
    savedFolder: string;
}

type myActions = {
    addRemoveFiles: (galleyId: string, images: File[], remove?: boolean) => { galleyId: string, images: File[], remove?: boolean };

    addRemoveMedia: (galleyId: string, list: MediaFileBaseModel[], remove?: boolean) => { galleyId: string, list: MediaFileBaseModel[], remove?: boolean };

    updateMediaObject: (galleyId: string, newObject: MediaFileBaseModel) => { galleyId: string, newObject: MediaFileBaseModel};

    clearList: (galleyId: string) => string;

    setDragOver: (galleyId: string, value: boolean) => { galleyId: string, value: boolean };

    selectObjectType: (value?: string) => string;
}


function readableBytes(bytes: number) {

    if (!bytes)
        return '0 KB';

    const i = Math.floor(Math.log(bytes) / Math.log(1024)),
        sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

    const r = (bytes / Math.pow(1024, i)).toFixed(2);

    return r + ' ' + sizes[i];
}


class galleryReducer extends ReducerBase<IMediaFilesState, myActions>{
    createActionList() {
        return {
            addRemoveFiles: (galleyId: string, images: File[], remove?: boolean) => ({ galleyId, images, remove }),
            addRemoveMedia: (galleyId: string, list: MediaFileBaseModel[], remove?: boolean) => ({ galleyId, list, remove }),

            updateMediaObject: (galleyId: string, newObject: MediaFileBaseModel) => ({ galleyId, newObject}),

            clearList: (galleyId: string) => galleyId,

            setDragOver: (galleyId: string, value: boolean) => ({ galleyId, value }),

            selectObjectType: (value?: string) => value,
        };
    }

    reducers() {

        const listHandlers = {};

        listHandlers[this._myActions.clearList.toString()] = (state, action: { payload: string }) => {
            const newState = _.clone(state || {}) as { [galleyId: string]: MediaObjects };
            delete newState[action.payload];
            return newState;
        }

        listHandlers[this._myActions.updateMediaObject.toString()] = (state, action: { payload: { galleyId: string, newObject: MediaFileBaseModel } }) => {
            const newState = _.clone(state || {}) as { [galleyId: string]: MediaObjects };

            const { galleyId, newObject } = action.payload;

            
            const list = newState[galleyId] && newState[galleyId].mediaList && newState[galleyId].mediaList[newObject.objectType];

            if (list) {
                const foundIndex = _.findIndex(list, l => l.path == newObject.path);
                if (-1 != foundIndex) {
                    list[foundIndex] = newObject;
                }
            }

            return newState;
        }


        listHandlers[this._myActions.addRemoveMedia.toString()] =
            (state, action: { payload: { galleyId: string, list: MediaFileBaseModel[], remove?: boolean } }) => {
                const newState = _.clone(state || {}) as { [galleyId: string]: MediaObjects };

                const { remove, galleyId, list } = action.payload;

                let newList = _.flatMap((newState[galleyId] && newState[galleyId].mediaList) || {});

                if (remove) {
                    newList = _.filter(newList, f => !_.includes(list, f));
                } else {
                    newList = _.concat(newList, list);
                }

                const mediaList = _.reduce(newList, (acc, o) => {

                    if (!!o) {
                        acc[o.objectType] = _.concat(acc[o.objectType] || [], o);
                    } 
                    return acc;
                }, {} as { [mediaType: string]: MediaFileBaseModel[] });

                newState[galleyId] = _.assign({}, newState[galleyId], { mediaList: mediaList });

                return newState;
            }


        listHandlers[this._myActions.addRemoveFiles.toString()] =
            (state, action: { payload: { galleyId: string, images: File[], remove?: boolean } }) => {
                const newState = _.clone(state || {}) as { [galleyId: string]: MediaObjects };

                const { remove, galleyId, images } = action.payload;

                let files = (newState[galleyId] && newState[galleyId].files) || [];

                if (remove) {
                    files = _.filter(files, f => !_.includes(images, f));
                } else {
                    files = _.concat(files, images);
                }

                newState[galleyId] = _.assign({}, newState[galleyId], { files: files });

                return newState;
        }


        return {

            hasDragOver: handleAction(this._myActions.setDragOver, (state, action) => {
                const newState = _.clone(state || {}) as { [galleyId: string]: boolean };
                const payload = action.payload as { galleyId: string, value: boolean };

                if (payload.value)
                    newState[payload.galleyId] = true;
                else
                    delete newState[payload.galleyId];

                return newState;

            }, {}),

            selectedObjectType: handleAction(this._myActions.selectObjectType, (state, action) => action.payload,null),

            objectList: handleActions(listHandlers, {}),
        };
    }

    setDragOver = (galleyId: string, value: boolean) => this._myActions.setDragOver(galleyId, value);

    addRemoveFiles(galleyId: string, images: File[], remove?: boolean, imageSaver?: (files: File[]) => PromiseLike<MediaFileBaseModel[]>) {
        return async (dispatch, getState) => {
            
            if (!remove && !!imageSaver) {
                const imageList = await imageSaver(images);
                dispatch(this._myActions.addRemoveMedia(galleyId, imageList));
            } else {
                dispatch(this._myActions.addRemoveFiles(galleyId, images, remove));
            }
            
        };
    }
   
    addRemoveMedia(galleyId: string, list: MediaFileBaseModel[], remove?: boolean, remover?: (listf: MediaFileBaseModel[]) => void) {
        
        return async (dispatch, getState) => {
            if (remove && remover)
                dispatch(remover(list));

            dispatch(this._myActions.addRemoveMedia(galleyId, list, remove))
        };
        
    }

    updateMediaObject = (galleyId: string, newObject: MediaFileBaseModel) => this._myActions.updateMediaObject(galleyId, newObject);

    selectObjectType = (value?: string) => this._myActions.selectObjectType(value);

    clearFiles = (galleyId: string) => this._myActions.clearList(galleyId);

    finalizeFilesAsync(galleyId: string, folderName: string, filesUnOrdered?:File[]) {
        const _mine = this;
        return async (dispatch, getState) => {

            let uploadWaitBox: string = null;
            let mediaFiles: MediaFileBaseModel[] = null;

            let filesystemLink: string = '';
            let savedFolder: string = null;

            const waiter = dispatch(ensureWaitBox().InitWait('uploading files', (async () => {

                if (!filesUnOrdered) {
                    const { objectList } = _mine.getCurrentState(getState());

                    filesUnOrdered = objectList && objectList[galleyId] && objectList[galleyId].files;
                }

                if (!filesUnOrdered || filesUnOrdered.length < 1)
                    throw 'no files to upload';

                if (filesUnOrdered.length > 50)
                    throw 'max 50 files at a time';

                mediaFiles = await Promise.all(filesUnOrdered.map(async (file, i) => {

                    if (file.size > 1024 * 1024 * 1024 * 100) {
                        throw 'max file size is 1 GB';
                    }

                    
                    //let fileName = file.name.replace(/ /g, "_");

                    const url = `/api/media/newImageId?fileType=${encodeURIComponent(file['type'])}`
                        + `&fileName=${encodeURIComponent(file.name)}`
                        + `&folderpath=${encodeURIComponent(folderName)}`;
                        
                    const uploadData = await ((await checkFetchError(await fetch(url))).json() as Promise<DirectUploadModel>);    

                    //we will set this few times no isses
                    savedFolder = uploadData.rootFolder;
                    //filesystemLink = `${uploadData.config.filesystemLink}/${savedFolder}`;

                    const uploader = await Evaporate.create(_.assign(uploadData.config, {

                        customAuthMethod: async (signParams, signHeaders, stringToSign, signatureDateTime, canonicalRequest) => {

                            const toSign = `/api/media/uploadSignature?to_sign=${stringToSign}`
                                + `&datetime=${signatureDateTime}`
                                + `&canonical_request=${encodeURIComponent(canonicalRequest)}`;

                            const signed = await ((await checkFetchError(await fetch(toSign))).text() as Promise<string>);    
                            return signed;
                        },

                        cloudfront: false,
                        logging: false,
                        computeContentMd5: true,
                        cryptoMd5Method: (d) => btoa(sparkMD5.ArrayBuffer.hash(d, true)),
                        cryptoHexEncodedHash256: sha256
                    }));

                    await uploader.add({
                        file: file,
                        name: uploadData.keyForDirectUpload,
                        progress: (percent, stats) => {
                            //console.log('Progress', percent, stats);
                            if (uploadWaitBox) {
                                setTimeout(() =>
                                    dispatch(ensureWaitBox().setProgress(uploadWaitBox, {
                                        status: stats && `${readableBytes(stats.totalUploaded)} of ${readableBytes(stats.fileSize)} @ ${stats.readableSpeed}/s)`,
                                        percent: (percent || 0) * 100
                                    })), 10);
                            }
                        },
                        complete: (xhr, awsObjectKey) => console.log('fileUpload s3 Complete!', file.name, uploadData.keyForDirectUpload),
                        error: (mssg) => {

                            console.error('Error', mssg)
                        },
                        paused: () => console.log('s3 upload Paused'),
                        pausing: () => console.log('s3 upload Pausing'),
                        resumed: () => console.log('s3 upload Resumed'),
                        cancelled: () => console.log('s3 upload Cancelled'),
                        started: (fileKey) => console.log('fileUpload s3 Started', file.name, fileKey),
                        uploadInitiated: (s3Id) => console.log('Upload Initiated', s3Id),
                        warn: (mssg) => {

                            console.log('Warning', mssg);
                            uploader.cancel(`${uploadData.config.bucket}/${uploadData.keyForDirectUpload}`);
                        }
                    });

                    return uploadData.mediaFile;

                }));

                return true;
            })()));
            uploadWaitBox = waiter.waitId;

            await waiter.waitPromise;

            return { mediaFiles, filesystemLink, savedFolder} as SavedMediaProps;
        };
    }
}

export default () => galleryReducer.getInstance(galleryReducer, 'mediaList');
