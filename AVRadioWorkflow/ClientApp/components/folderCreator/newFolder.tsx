import * as React from 'react';

import { Card, Form, Row, Col, InputGroup, Button, Alert } from 'react-bootstrap';

import { withRouter, RouteComponentProps } from 'react-router-dom';

import DatePicker from 'react-datepicker';
import "react-datepicker/dist/react-datepicker.css";

import ensureCreator, { ICreatorState } from './reducer';

import queryString from 'query-string';

import MediaList from '../mediaList';

import "./styles.scss";


type ConnectedProps = ICreatorState;

type ComponentProps = RouteComponentProps<{}>;

class FolderCreatorView extends React.PureComponent<ConnectedProps & ComponentProps & { dispatch }, {}>{
    componentDidMount() {
        const { dispatch} = this.props;
        dispatch(ensureCreator().loadStuff());
    }

    render() {
        const { createOptions, folderDetails, dispatch, history } = this.props;


        return <div className="folderCreator">

            <Card >
                <Card.Header>Create a new folder</Card.Header>
                <Card.Body>
                    <Form
                        onSubmit={async e => {
                            e.preventDefault();
                            const savedFolder = await dispatch(ensureCreator().createNewFolderAsync());
                            history.push(`/publiMe?filename=${encodeURIComponent(savedFolder)}`);
                        }}
                    >
                        <fieldset>
                            <Row>
                                <Col md >
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text >Recording Date*</InputGroup.Text>
                                            </InputGroup.Prepend>

                                            <DatePicker required
                                                selected={folderDetails && folderDetails.recordingDate || null}
                                                onChange={e => {


                                                    return dispatch(ensureCreator().updateFolderProps('recordingDate', e));
                                                }}
                                            />
                                        </InputGroup>
                                    </Form.Group>
                                </Col>
                                <Col md>
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text>Genre</InputGroup.Text>
                                            </InputGroup.Prepend>
                                            <Form.Control as="select"
                                                placeholder="choose the genre" required
                                                value={folderDetails && folderDetails.genre || ''}
                                                onChange={(e: any) => dispatch(ensureCreator().updateFolderProps('genre', e.target.value))}
                                            >
                                                {(createOptions && createOptions.availableGenres || []).map((o, i) => <option key={i}>{o}</option>)}
                                            </Form.Control>
                                        </InputGroup>
                                    </Form.Group>

                                </Col>
                            </Row>

                            <Row>
                                <Col md >
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text>Language</InputGroup.Text>
                                            </InputGroup.Prepend>
                                            <Form.Control as="select"
                                                placeholder="choose the language" required

                                                value={folderDetails && folderDetails.language || ''}
                                                onChange={(e: any) => dispatch(ensureCreator().updateFolderProps('language', e.target.value))}
                                            >
                                                {(createOptions && createOptions.availableLanguage || []).map((o, i) => <option key={i}>{o}</option>)}
                                            </Form.Control>
                                        </InputGroup>
                                    </Form.Group>
                                </Col>
                                <Col md>
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text >Recorded by</InputGroup.Text>
                                            </InputGroup.Prepend>

                                            <Form.Control type="text" placeholder="recording engineer's name (optional)"
                                                value={folderDetails && folderDetails.recordingBy || ''}
                                                onChange={e => dispatch(ensureCreator().updateFolderProps('recordingBy', e.target.value))}
                                            />
                                        </InputGroup>
                                    </Form.Group>

                                </Col>
                            </Row>

                            <Row>
                                <Col >
                                    <Form.Group>
                                        <InputGroup >
                                            <InputGroup.Prepend>
                                                <InputGroup.Text >Description*</InputGroup.Text>
                                            </InputGroup.Prepend>

                                            <Form.Control type="text" placeholder="description please" required
                                                value={folderDetails && folderDetails.description || ''}
                                                onChange={e => dispatch(ensureCreator().updateFolderProps('description', e.target.value))}

                                            />
                                        </InputGroup>
                                    </Form.Group>
                                </Col>
                            </Row>

                            <MediaList galleryId="folderCreator" />

                            <div className="footer">
                                <Button type="submit" variant="primary" block>Save folder</Button>
                            </div>

                        </fieldset>
                    </Form>
                </Card.Body>
            </Card >
        </div>;
    }
}

import { connect } from 'react-redux';

export default withRouter(connect<ConnectedProps, { dispatch }, {}>((state) => {
    return ensureCreator().getCurrentState(state);
})(FolderCreatorView));