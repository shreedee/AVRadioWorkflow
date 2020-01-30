import ReducerBase from '../../bootCommon/baseReducer';
import { handleAction, handleActions } from 'redux-actions';

import * as _ from 'lodash'; 

import { FolderDetailsModel } from '../../generated/FolderDetailsModel';
import { CreateOptionModel } from '../../generated/CreateOptionModel';


import { checkFetchError } from '../../bootCommon/asyncLoader';

import ensureWaitBox from '../waitBox/reducer';
import ensureMedia, { SavedMediaProps } from '../mediaList/reducer';

import moment from 'moment';
import { setTimeout } from 'timers';

export interface ICreatorState {
    readonly folderDetails: FolderDetailsModel;
    readonly createOptions: CreateOptionModel;

    
};

type myActions = {
    loaddetails: (value: FolderDetailsModel) => FolderDetailsModel;

    updateFolderProps: (key: keyof FolderDetailsModel, value: any) => { key: keyof FolderDetailsModel, value: any };

    loadOptions: (value: CreateOptionModel) => CreateOptionModel;

    
}

class creatorReducer extends ReducerBase<ICreatorState, myActions>{

    createActionList() {
        return {
            loaddetails: (value: FolderDetailsModel) => value,
            updateFolderProps: (key: keyof FolderDetailsModel, value: any) => ({ key, value }),

            loadOptions: (value: CreateOptionModel) => value,

            

        };
    }

    reducers() {

        const detailsHandlers = {};

        detailsHandlers[this._myActions.loaddetails.toString()] = (state, action) => action.payload;

        detailsHandlers[this._myActions.updateFolderProps.toString()] = (state, action) => {

            const newState = _.clone(state || {}) as FolderDetailsModel;

            const payload = action.payload as { key: keyof FolderDetailsModel, value: any };

            newState[payload.key] = payload.value;

            return newState;

        };

        return {
            folderDetails: handleActions(detailsHandlers, null),
            createOptions: handleAction(this._myActions.loadOptions, (state, action) => action.payload, null),
            
        };

    }

     

    createNewFolderAsync() {
        const _mine = this;
        return async (dispatch, getState) => {
            const { folderDetails } = _mine.getCurrentState(getState());

            const { genre, language, recordingDate, recordingBy, description } = folderDetails;

            const recDate = moment(recordingDate);
            //const desc_replaced = (description||'').replace(/ /g, "_");

            const savedFolder = (`${recDate.format('YYYY_MM_DD')}_${genre}_${(description || '')}_${language}`
                + (recordingBy ? ` (${recordingBy})` : ''))
                .replace(/[|&;$%@"<>()+,]/g, "")
                .replace(/ /g, "_");

            //this shows it's own wait box
            const { mediaFiles, filesystemLink } = (await dispatch(ensureMedia().finalizeFilesAsync('folderCreator', savedFolder))) as SavedMediaProps;

            await dispatch(ensureWaitBox().doWaitAsync('saving data', async () => {

                await checkFetchError(await fetch('/api/foldercreator/newFolder', {
                    method: 'post',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(_.assign({}, folderDetails, { savedFolder, publishDetails: { mediaFiles } }))
                }));

                return true;
            }));

            return filesystemLink;
        };
    }

    updateFolderProps = (key: keyof FolderDetailsModel, value: any) => this._myActions.updateFolderProps(key, value);

    loadStuff(filename?:string) {
        const _mine = this;
        return (dispatch, getState) => {
            dispatch(ensureWaitBox().doWaitAsync('loading options', async () => {

                dispatch(ensureMedia().clearFiles('folderCreator'));

                const options = await ((await checkFetchError(await fetch('/api/foldercreator/options'))).json() as Promise<CreateOptionModel>);
                dispatch(_mine._myActions.loadOptions(options));

                const details = filename ?
                    await ((await checkFetchError(await fetch(`/api/foldercreator/load?filename=${encodeURIComponent(filename)}`))).json() as Promise<FolderDetailsModel>)
                    : {
                        description: null,
                        recordingDate: new Date(),
                        genre: options.defaultGenre,
                        language: options.defaultLanguage,

                    };

                //to convert the serialzed date to real Date
                details.recordingDate = new Date(details.recordingDate);

                dispatch(_mine._myActions.loaddetails(details));

                return true;
            }));
        };

    }

}

export default () => creatorReducer.getInstance(creatorReducer, 'foldercreator');