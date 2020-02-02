import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import * as _ from 'lodash'; 

import { FolderDetailsModel } from '../../generated/FolderDetailsModel';
import { PublishDetailsModel } from '../../generated/PublishDetailsModel';
import { FolderDataModel } from '../../generated/FolderDataModel';

import { MediaFileBaseModel } from '../../generated/MediaFileBaseModel';


import { checkFetchError } from '../../bootCommon/asyncLoader';

import ensureWaitBox from '../waitBox/reducer';
import ensureMedia, { SavedMediaProps } from '../mediaList/reducer';

import moment from 'moment';
import { setTimeout } from 'timers';

export type ICreatorState = FolderDataModel & {
    
};

type myActions = {
    loadData: (value: FolderDataModel) => FolderDataModel;
    updateFolderProps: (key: keyof FolderDetailsModel, value: any) => { key: keyof FolderDetailsModel, value: any };

    updatePublishProps: (key: keyof PublishDetailsModel, value: any) => { key: keyof PublishDetailsModel, value: any };
}

class creatorReducer extends ReducerBase<ICreatorState, myActions>{

    createActionList() {
        return {
            loadData: (value: FolderDataModel) => value,

            updateFolderProps: (key: keyof FolderDetailsModel, value: any) => ({ key, value }),
            updatePublishProps: (key: keyof PublishDetailsModel, value: any) => ({ key, value })
        };
    }

    reducers() {

        const detailsHandlers = {};

        detailsHandlers[this._myActions.loadData.toString()] = (state, action: { payload: FolderDataModel }) => {
            let ret = (action.payload && action.payload.folderDetails) || null;

            //to convert the serialzed date to real Date
            if (ret && ret.recordingDate)
                ret = _.assign({}, ret, { recordingDate: new Date(ret.recordingDate) });

            return ret;
        }


        detailsHandlers[this._myActions.updateFolderProps.toString()] = (state, action) => {

            const newState = _.clone(state || {}) as FolderDetailsModel;

            const payload = action.payload as { key: keyof FolderDetailsModel, value: any };

            newState[payload.key] = payload.value;

            return newState;

        };

        detailsHandlers[this._myActions.updatePublishProps.toString()] = (state, action) => {

            const newState = _.clone(state || {}) as FolderDetailsModel;
            newState.publishDetails = _.clone(newState.publishDetails || {}) as PublishDetailsModel;

            const payload = action.payload as { key: keyof PublishDetailsModel, value: any };

            newState.publishDetails[payload.key] = payload.value;

            return newState;

        };

        return {
            folderDetails: handleActions(detailsHandlers, null),
            createOptions: handleAction(this._myActions.loadData, (state, action: { payload: FolderDataModel }) => (action.payload && action.payload.createOptions) || null, null),
            externalLink: handleAction(this._myActions.loadData, (state, action: { payload: FolderDataModel }) => (action.payload && action.payload.externalLink) || null, null),
        };

    }

    addToMedia(files:File[]) {
        const _mine = this;
        return async (dispatch, getState) => {

            const { folderDetails } = _mine.getCurrentState(getState());
            if (!folderDetails.savedFolder)
                throw 'Folder not yet initialized';

            const { mediaFiles, filesystemLink, savedFolder } = (await dispatch(ensureMedia().finalizeFilesAsync('folderCreator',

                folderDetails.savedFolder, files

            ))) as SavedMediaProps;

            await dispatch(_mine.saveCurrentChanges(mediaFiles));

            return mediaFiles;
        };
    }

    saveCurrentChanges(newMediaFiles?: MediaFileBaseModel[]) {
        const _mine = this;
        return async (dispatch, getState) => {

            await dispatch(ensureWaitBox().doWaitAsync('saving data', async () => {

                {
                    const { folderDetails } = _mine.getCurrentState(getState());

                    if (!!newMediaFiles) {
                        const mediaFiles = folderDetails && folderDetails.publishDetails && folderDetails.publishDetails.mediaFiles || [];
                        dispatch(_mine.updatePublishProps('mediaFiles', _.concat(mediaFiles, newMediaFiles)));
                    }
                }

                {
                    const { folderDetails } = _mine.getCurrentState(getState());

                    await (await checkFetchError(await fetch('/api/foldercreator/save', {
                        method: 'post',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(folderDetails)
                    }))).text();
                }

                return true;

            }));
        };
    }

    createNewFolderAsync() {
        const _mine = this;
        return async (dispatch, getState) => {
            const { folderDetails } = _mine.getCurrentState(getState());

            const { genre, language, recordingDate, recordingBy, description } = folderDetails;

            const recDate = moment(recordingDate);
            //const desc_replaced = (description||'').replace(/ /g, "_");

            /*
            const savedFolder = (`${recDate.format('YYYY_MM_DD')}_${genre}_${(description || '')}_${language}`
                + (recordingBy ? ` (${recordingBy})` : ''))
                .replace(/[|&;$%@"<>()+,]/g, "")
                .replace(/ /g, "_")
                ;
                */

            //this shows it's own wait box
            const { mediaFiles, filesystemLink, savedFolder } = (await dispatch(ensureMedia().finalizeFilesAsync('folderCreator',

                (`${recDate.format('YYYY_MM_DD')}_${genre}_${(description || '')}_${language}`
                + (recordingBy ? ` (${recordingBy})` : ''))

            ))) as SavedMediaProps;

            let statusFile = '';
            await dispatch(ensureWaitBox().doWaitAsync('saving data', async () => {

                statusFile = await (await checkFetchError(await fetch('/api/foldercreator/newFolder', {
                    method: 'post',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(_.assign({}, folderDetails, { savedFolder, publishDetails: { mediaFiles } }))
                }))).text();

                return true;
            }));

            return statusFile;
        };
    }

    updateFolderProps = (key: keyof FolderDetailsModel, value: any) => this._myActions.updateFolderProps(key, value);
    updatePublishProps= (key: keyof PublishDetailsModel, value: any) => this._myActions.updatePublishProps(key, value);
    

    loadStuff(filename?:string) {
        const _mine = this;
        return async (dispatch, getState) => {

            await dispatch(ensureWaitBox().doWaitAsync('loading options', async () => {

                dispatch(ensureMedia().clearFiles('folderCreator'));

                const data = await ((await checkFetchError(await fetch(`/api/foldercreator/load?filename=${encodeURIComponent(filename || '')}`))).json() as Promise<FolderDataModel>);
                dispatch(_mine._myActions.loadData(data));


                dispatch(ensureMedia().addRemoveMedia('folderCreator',
                    data.folderDetails && data.folderDetails.publishDetails && data.folderDetails.publishDetails.mediaFiles));

                dispatch(ensureMedia().selectObjectType('ImageFileModel'));

                return true;
            }));
        };

    }

}

export default () => creatorReducer.getInstance(creatorReducer, 'foldercreator');