import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import { checkFetchError } from '../../bootCommon/asyncLoader';
import ensureWaitBox from '../waitBox/reducer';
import * as sparkMD5 from 'spark-md5';
import * as sha256 from 'js-sha256';


import * as Evaporate from '../../../pullRequest/evaporateJS';

import { DirectUploadModel } from '../../generated/DirectUploadModel';
import { MediaFileBaseModel } from '../../generated/MediaFileBaseModel'

import * as _ from 'lodash';

export interface IMediaFilesState {
    readonly fileList: { [galleyId: string]: File[] };
    readonly hasDragOver: { [galleyId: string]: boolean };
};


export type SavedMediaProps = {
    mediaFiles: MediaFileBaseModel[];
    filesystemLink: string;
}

type myActions = {
    updateFiles: (galleyId: string, images: File[], remove?: boolean) => { galleyId: string, images: File[], remove?: boolean };

    setDragOver: (galleyId: string, value: boolean) => { galleyId: string, value: boolean };
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
            updateFiles: (galleyId: string, images: File[], remove?: boolean) => ({ galleyId, images, remove }),
            setDragOver: (galleyId: string, value: boolean) => ({ galleyId, value })
        };
    }

    reducers() {

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

            fileList: handleAction(this._myActions.updateFiles, (state, action) => {
                const newState = _.clone(state || {}) as { [galleyId: string]: File[] };
                const payload = action.payload as { galleyId: string, images: File[], remove?: boolean };

                if (payload.remove) {
                    newState[payload.galleyId] = _.filter(newState[payload.galleyId] || [], f => !_.includes(payload.images, f));
                } else {
                    newState[payload.galleyId] = _.concat(newState[payload.galleyId] || [], payload.images);
                }
                return newState;
            }, {})
        };
    }

    setDragOver = (galleyId: string, value: boolean) => this._myActions.setDragOver(galleyId, value);

    updateFiles = (galleyId: string, images: File[], remove?: boolean) => this._myActions.updateFiles(galleyId, images, remove);

    clearFiles(galleyId: string) {
        const _mine = this;
        return (dispatch, getState) => {
            const { fileList } = _mine.getCurrentState(getState());
            const filesUnOrdered = fileList && fileList[galleyId];
            return dispatch(_mine.updateFiles(galleyId, filesUnOrdered, true));
        };
    }

     finalizeFilesAsync(galleyId: string, folderName:string) {
        const _mine = this;
        return async (dispatch, getState) => {

            let uploadWaitBox: string = null;
            let mediaFiles: MediaFileBaseModel[] = null;

            let filesystemLink: string = null;

            const waiter = dispatch(ensureWaitBox().InitWait('uploading files', (async () => {

                const { fileList } = _mine.getCurrentState(getState());
                const filesUnOrdered = fileList && fileList[galleyId];

                if (!filesUnOrdered || filesUnOrdered.length < 1)
                    throw 'no files to upload';

                if (filesUnOrdered.length > 50)
                    throw 'max 50 files at a time';

                mediaFiles = await Promise.all(filesUnOrdered.map(async (file, i) => {

                    if (file.size > 1024 * 1024 * 100) {
                        throw 'max file size is 100 MB';
                    }

                    
                    let fileName = file.name.replace(/ /g, "_");

                    const url = `/api/media/newImageId?fileType=${encodeURIComponent(file['type'])}`
                        + `&fileName=${encodeURIComponent(fileName)}`
                        + `&folderpath=${encodeURIComponent(folderName)}`;
                        
                    const uploadData = await ((await checkFetchError(await fetch(url))).json() as Promise<DirectUploadModel>);    

                    //we will set this few times no isses
                    filesystemLink = `${uploadData.config.filesystemLink}/${folderName}`;

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

            return { mediaFiles, filesystemLink } as SavedMediaProps;
        };
    }
}

export default () => galleryReducer.getInstance(galleryReducer, 'mediaList');
