import * as React from 'react';
import { FormControl, ListGroup, Button, OverlayTrigger, Tooltip, Card, Accordion, Badge, Image, FormCheck } from 'react-bootstrap';

import ensureMedia, { IMediaFilesState } from './reducer';

import ensureCreator from '../folderCreator/reducer';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFileArchive } from '@fortawesome/free-solid-svg-icons';

import { MediaFileBaseModel } from '../../generated/MediaFileBaseModel';
import { ImageFileModel } from '../../generated/ImageFileModel';

import * as _ from 'lodash';

import "./styles.scss";

type ComponentProps = {
    galleryId: string;
    imageSaver?: {
        saver: (files: File[]) => PromiseLike<MediaFileBaseModel[]>;
        remover: (list: MediaFileBaseModel[])=>void
    };
    rootPath?: string;
};

type ConnectedProps = IMediaFilesState;

let _fileRenderIdCounter = 0;

const MediaListView: React.SFC<ComponentProps & ConnectedProps & { dispatch }> = ({ objectList, galleryId, dispatch, hasDragOver,
    selectedObjectType, imageSaver,
    rootPath
}) => {
    ///the hidden file form Element
    let _fileInputRef = null;

    const files = objectList && objectList[galleryId] && objectList[galleryId].files || [];

    const mediaParts = objectList && objectList[galleryId] && objectList[galleryId].mediaList || {};

    const validMediaPartKeys = _.filter(_.keys(mediaParts), k => mediaParts[k].length > 0);

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

            dispatch(ensureMedia().setDragOver(galleryId, false));

            if (e.dataTransfer.files)
                dispatch(ensureMedia().addRemoveFiles(galleryId, [...e.dataTransfer.files], false, imageSaver && imageSaver.saver));

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
                dispatch(ensureMedia().addRemoveFiles(galleryId, [...e.nativeEvent.target.files], false, imageSaver && imageSaver.saver));
            }}
        />

        <Card.Body className={'dropContainer ' + (ihaveDrag ? 'hasDrag' : '')}>

            <Accordion activeKey={selectedObjectType} >
                {validMediaPartKeys.map(mp => < Card key = { mp } >
                    <Card.Header>
                        <Accordion.Toggle as={Button} variant="link"
                            eventKey={mediaParts[mp][0].objectType}
                            onClick={() => dispatch(ensureMedia().selectObjectType(mediaParts[mp][0].objectType))}
                        >
                            {mediaParts[mp][0].fileType}
                        </Accordion.Toggle>

                        <Badge>{mediaParts[mp].length}</Badge>

                    </Card.Header>
                    <Accordion.Collapse eventKey={mediaParts[mp][0].objectType}>
                        <Card.Body>
                            <ListGroup >
                                {mediaParts[mp].map((f, i) => {

                                    let variant = null;
                                    if (f.canPublish) {
                                        if ( !f.doNotPublish) {
                                            variant = 'success';
                                        }
                                    } else {
                                        if ('ImageFileModel' == f.objectType) {
                                            variant = 'danger';
                                        }
                                            
                                    }

                                    return <ListGroup.Item key={i}
                                        variant={variant}>

                                        <div className="d-flex">
                                            {(() => {
                                                switch (f.objectType) {
                                                    case 'ImageFileModel':
                                                        return <div className="d-flex" >

                                                            {rootPath && <div style={{ height: 100, width: 100 }}>
                                                                <Image src={`${rootPath}/${f.path}`} fluid />
                                                            </div>
                                                            }

                                                            <div className="fileDisplay">
                                                                <div>{f.fileName}</div>
                                                                {!(f as ImageFileModel).canPublish && <div className="text-muted">incorrect aspect ratio</div>}
                                                            </div>
                                                        </div>;
                                                    default:
                                                        return <div className="fileDisplay">
                                                            {f.fileName}
                                                        </div>;
                                                }
                                            })()}

                                            <div className="ml-auto">

                                                {f.canPublish && <FormCheck inline  type="checkbox" label="Do NOT publish"
                                                    checked={f.doNotPublish}
                                                    onClick={() => dispatch(ensureCreator().toggglePublish(f.path))}
                                                    onChange={() => { }}
                                                />}

                                                <Button title="remove file" variant="danger" size="sm"
                                                    onClick={() => dispatch(ensureMedia().addRemoveMedia(galleryId, [f], true, imageSaver && imageSaver.remover))}
                                                    
                                                >X</Button>
                                            </div>
                                        </div>

                                    </ListGroup.Item>;
                                })}
                            </ListGroup>
                        </Card.Body>
                    </Accordion.Collapse>
                </Card>)}
            </Accordion>

            <ListGroup >
                {files.map((f, i) => <ListGroup.Item key={i}>

                    <div className="d-flex">
                        <div className="mr-auto">{f.name}</div>
                        <div >
                            <Button title="remove file" variant="danger" size="sm"
                                onClick={() => dispatch(ensureMedia().addRemoveFiles(galleryId, [f], true))}
                            >X</Button>
                        </div>
                    </div>

                </ListGroup.Item>)}

                {files.length == 0 && validMediaPartKeys.length ==0 && < ListGroup.Item > <span className="text-muted">Drag files here</span></ListGroup.Item>}
            </ListGroup>
        </Card.Body>


    </Card >;

};

import { connect } from 'react-redux';


export default connect<ConnectedProps, { dispatch }, ComponentProps>((state) => {
    return ensureMedia().getCurrentState(state);
})(MediaListView);