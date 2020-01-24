import * as React from 'react';
import { FormControl, ListGroup, Button, OverlayTrigger, Tooltip, Card  } from 'react-bootstrap';

import ensureMedia, { IMediaFilesState } from './reducer';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFileArchive } from '@fortawesome/free-solid-svg-icons';


import * as _ from 'lodash';

import "./styles.scss";

type ComponentProps = {
    galleryId: string;
};

type ConnectedProps = IMediaFilesState;

let _fileRenderIdCounter = 0;


const MediaListView: React.SFC<ComponentProps & ConnectedProps & { dispatch }> = ({ fileList, galleryId, dispatch, hasDragOver }) => {
    ///the hidden file form Element
    let _fileInputRef = null;

    const files = fileList && fileList[galleryId] || [];

    const ihaveDrag = hasDragOver && hasDragOver[galleryId];

    return <Card className="mediaList"
        onDragOver={e => {
            e.stopPropagation();
            event.preventDefault();
            e.dataTransfer.dropEffect = "move"
        }}

        onDragEnter={e => {
            e.stopPropagation();
            event.preventDefault();
            dispatch(ensureMedia().setDragOver(galleryId, true));
        }}

        onDragLeave={e => {
            e.stopPropagation();
            dispatch(ensureMedia().setDragOver(galleryId, false));
        }}

        onDrop={e => {
            e.stopPropagation();
            e.preventDefault();

            if (e.dataTransfer.files)
                dispatch(ensureMedia().updateFiles(galleryId, [...e.dataTransfer.files]));

            return false;
        }}
        >
        <Card.Header>
            <div className="d-flex">
                <div className="mr-auto"><strong>List of files</strong></div>
                <div >
                    <Button variant="outline-info"
                        onClick={() => _fileInputRef && _fileInputRef.click()}
                    >
                        <span><FontAwesomeIcon icon={faFileArchive} />Add files </span>
                    </Button>
                </div>
            </div>
        </Card.Header>

        <FormControl
            ref={(ref => { _fileInputRef = ref; }) as any}
            type="file" multiple id={`myinputfile_${_fileRenderIdCounter++}`}
            style={{ height: 0, width: 0, opacity: 0, display: 'contents' }} onChange={(e: any) => {
                e.preventDefault();
                //the event is of type FIleList we need to convert it to file[]
                dispatch(ensureMedia().updateFiles(galleryId, [...e.nativeEvent.target.files]));
            }}
        />

        <ListGroup className={'theList ' + (ihaveDrag?'hasDrag':'')}>
            {files.map((f, i) => <ListGroup.Item key={i}>
                <div className="d-flex">
                    <div className="mr-auto">{f.name}</div>
                    <div >
                        <Button title="remove file" variant="danger" size="sm"
                            onClick={() => dispatch(ensureMedia().updateFiles(galleryId,[f],true))}
                        >X</Button>
                    </div>
                </div>
                
            </ListGroup.Item>)}

            {files.length == 0 && <ListGroup.Item><span className="text-muted">Drag files here</span></ListGroup.Item>}
        </ListGroup>

        
    </Card >;

};

import { connect } from 'react-redux';

export default connect<ConnectedProps, { dispatch }, ComponentProps>((state) => {
    return ensureMedia().getCurrentState(state);
})(MediaListView);