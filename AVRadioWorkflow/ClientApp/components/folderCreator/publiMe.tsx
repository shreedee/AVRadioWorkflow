import React from 'react';

import { Card, Form, Row, Col, InputGroup, Button, Alert, ButtonToolbar  } from 'react-bootstrap';

import { withRouter, RouteComponentProps } from 'react-router-dom';

import DatePicker from 'react-datepicker';
import "react-datepicker/dist/react-datepicker.css";

import ensureCreator, { ICreatorState } from './reducer';

import queryString from 'query-string';

import MediaList from '../mediaList';

import "./styles.scss";

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTwitter, faYoutube } from '@fortawesome/free-brands-svg-icons';

import * as _ReactQuill from 'react-quill';
const ReactQuill = _ReactQuill as any;

import 'react-quill/dist/quill.snow.css';

import ToggleButton from 'react-toggle-button';
import { PublishedLinkModel } from '../../generated/PublishedLinkModel';

import { PublishStatusModel } from '../../generated/PublishStatusModel';
import moment from 'moment';


type ConnectedProps = ICreatorState;

type ComponentProps = RouteComponentProps<{}>;

class PubliMeView extends React.PureComponent<ConnectedProps & ComponentProps & { dispatch }, {}>{
    async componentDidMount() {
        const { dispatch, location, history } = this.props;

        const { filename } = queryString.parse(location && location.search);
        if (!filename) {
            history.push('foldercreator');
        } else {

            //if we fail to load the file go to
            try {
                await dispatch(ensureCreator().loadStuff(filename as string));
            } catch{
                history.push('foldercreator');
            }

        }

    }

    render() {

        const { folderDetails, dispatch, createOptions, displayData, history,  } = this.props;
    
        const publishDetails = folderDetails && folderDetails.publishDetails;
        const publishedLink :PublishedLinkModel = null;//folderDetails && folderDetails.publishedLink;


        return <div className="folderCreator">

            {displayData && displayData.externalLink && <Alert variant="info">
                fileSystem link : {displayData.externalLink}
            </Alert>
            }

            {publishedLink && <Alert variant="primary">
                Last <Alert.Link href={publishedLink.wpLink}> published</Alert.Link> @{publishedLink.lastModified}
            </Alert>}

            <Form
                onSubmit={async e => {
                    e.preventDefault();
                    const savedFolder = await dispatch(ensureCreator().publish());
                    await dispatch(ensureCreator().loadStuff(savedFolder as string));
                    history.push(`/publiMe?filename=${encodeURIComponent(savedFolder)}`);
                }}
            >

            <Row>
                <Col lg={6}>
                    <Card>
                        <Card.Header>Description</Card.Header>
                        <Card.Body>
                            <Form.Group>
                                <InputGroup >
                                    <InputGroup.Prepend>
                                        <InputGroup.Text >Title*</InputGroup.Text>
                                    </InputGroup.Prepend>

                                    <Form.Control type="text" placeholder="Title (required)"
                                        value={publishDetails && publishDetails.title || ''}
                                        onChange={e => dispatch(ensureCreator().updatePublishProps('title', e.target.value))}
                                    />

                                </InputGroup>
                            </Form.Group>
                            <Form.Group>
                                <InputGroup >
                                    <InputGroup.Prepend>
                                        <InputGroup.Text ><FontAwesomeIcon icon={faTwitter} /> Subtitle </InputGroup.Text>
                                    </InputGroup.Prepend>

                                    <Form.Control type="text" placeholder="Subtitle or twitter handle"
                                        value={publishDetails && publishDetails.twiterTitle || ''}
                                        onChange={e => dispatch(ensureCreator().updatePublishProps('twiterTitle', e.target.value))}
                                    />

                                </InputGroup>
                            </Form.Group>

                            <Form.Group>
                                <InputGroup >
                                    <InputGroup.Prepend>
                                        <InputGroup.Text >Program by</InputGroup.Text>
                                    </InputGroup.Prepend>

                                    <Form.Control type="text" placeholder="Program was created by"
                                        value={publishDetails && publishDetails.programBy || ''}
                                        onChange={e => dispatch(ensureCreator().updatePublishProps('programBy', e.target.value))}
                                    />

                                </InputGroup>
                            </Form.Group>

                            <div>
                                {publishDetails && <ReactQuill
                                    placeholder="post description"
                                    value={publishDetails && publishDetails.bodyText || ''}
                                    onChange={e => dispatch(ensureCreator().updatePublishProps('bodyText', e))}
                                />
                                }
                            </div>

                        </Card.Body>
                    </Card>
                </Col>
                <Col lg={6}>
                    <MediaList galleryId="folderCreator"
                        rootPath={folderDetails && folderDetails.savedFolder &&
                            displayData && displayData.httpLinkPrefix &&
                            `${displayData .httpLinkPrefix}/${folderDetails.savedFolder}`}
                        imageSaver={{
                            saver: files => dispatch(ensureCreator().addToMedia(files)),
                            remover: files => dispatch(ensureCreator().removeMedia(files))
                        }} />
                </Col>
            </Row>

            <Row>
                <Col lg>
                    <Card>
                        <Card.Header>Tags</Card.Header>
                        <Card.Body>
                            <Row>
                                <Col md>
                                    {createOptions && createOptions.availableCategories && createOptions.availableCategories.length > 0 && < Form.Group >
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text>Category</InputGroup.Text>
                                            </InputGroup.Prepend>
                                            <Form.Control as="select"
                                                placeholder="choose the category" required
                                                value={publishDetails && publishDetails.category || ''}
                                                onChange={(e: any) => dispatch(ensureCreator().updatePublishProps('category', e.target.value))}
                                            >
                                                <option></option>
                                                {(createOptions.availableCategories || []).map((o, i) => <option key={i}>{o}</option>)}
                                            </Form.Control>
                                        </InputGroup>
                                    </Form.Group>
                                    }
                                </Col>
                                <Col md>
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text >Language</InputGroup.Text>
                                            </InputGroup.Prepend>

                                            <Form.Control type="text"
                                                value={folderDetails && folderDetails.language || ''}
                                                readOnly
                                            />

                                        </InputGroup>
                                    </Form.Group>
                                </Col>
                            </Row>
                        </Card.Body>
                    </Card>
                </Col>
                <Col lg>
                    <Card>
                        <Card.Header>Social media</Card.Header>
                        <Card.Body>
                            <Row>
                                <Col xs="auto">
                                    <span className="text-muted">You Tube</span>
                                </Col>
                                <Col>
                                    <ToggleButton

                                        thumbIcon={<FontAwesomeIcon icon={faYoutube} />}

                                        value={false}
                                        onToggle={() => { }}
                                        disabled={true}
                                    />
                                </Col>
                            </Row>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>


                <div className="footer">

                    {((folderDetails && folderDetails.publishedActions) || []).map((a, i) => <ul key={i}>
                        <li>{a.message} @ {moment(a.lastModified).format('LLLL')}</li>
                    </ul>)
                    }

                    <ButtonToolbar className="justify-content-between">

                        <Button variant="info"
                            onClick={() => dispatch(ensureCreator().saveCurrentChanges())}
                        >Save folder</Button>

                    
                    {(folderDetails && folderDetails.publishStatus == PublishStatusModel.notPublished) ?
                        <Button type="submit" variant="primary">
                            PUBLISH
                        </Button>
                            :
                        <Button type="submit" variant="primary">
                                Update PUBLISH request
                        </Button>

                    
                    }

                    </ButtonToolbar>
                        
                
                </div>

            </Form>

        </div>;
    }
}

import { connect } from 'react-redux';


export default withRouter(connect<ConnectedProps, { dispatch }, {}>((state) => {
    return ensureCreator().getCurrentState(state);
})(PubliMeView));

